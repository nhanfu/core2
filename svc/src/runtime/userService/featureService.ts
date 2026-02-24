import path from "path";
import type { Component, Feature, FeaturePolicy } from "../types.js";
import type { UserServiceContext } from "./types.js";
import { combineStrings, escapeValue, getRowValue, isEmpty, toStringSafe } from "./utils.js";
import { parseJsonSafe } from "../utils.js";
import { readText, writeText, ensureDirectoryExists } from "./utils.js";

export class FeatureService {
  constructor(private context: UserServiceContext) {}

  async getFeature(name: string): Promise<Feature> {
    const feature = await this.context.metadataStore.getFeature(name, this.context.getRuntimeContext());
    if (!feature) throw new Error("Feature not found");
    return feature;
  }

  async publishAllFeature(tenant: string): Promise<boolean> {
    const conn = this.context.getDefaultConn();
    const [features, components, policies, settings] = await Promise.all([
      this.context.query("select * from [Feature]", conn),
      this.context.query(
        "select [Component].*, isnull(def.Value, DefaultVal) as DefaultVal, def.Id as ComponentDefaultValueId from [Component] outer apply (select top 1 Value,Id from ComponentDefaultValue where ComponentId = Component.Id) as def",
        conn,
      ),
      this.context.query("select * from [FeaturePolicy]", conn),
      this.context.query("select * from [UserSetting]", conn),
    ]);

    const featuresById = new Map<string, Feature>();
    features.forEach((row) => {
      const id = toStringSafe(getRowValue(row, "Id"));
      featuresById.set(id, row as Feature);
    });

    const componentsByFeature = new Map<string, Array<Record<string, unknown>>>();
    components.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!componentsByFeature.has(featureId)) componentsByFeature.set(featureId, []);
      componentsByFeature.get(featureId)?.push(row);
    });

    const policiesByFeature = new Map<string, FeaturePolicy[]>();
    policies.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!policiesByFeature.has(featureId)) policiesByFeature.set(featureId, []);
      policiesByFeature.get(featureId)?.push(row as FeaturePolicy);
    });

    const settingsByFeature = new Map<string, Array<Record<string, unknown>>>();
    settings.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!settingsByFeature.has(featureId)) settingsByFeature.set(featureId, []);
      settingsByFeature.get(featureId)?.push(row as Record<string, unknown>);
    });

    for (const feature of featuresById.values()) {
      const featureId = feature.Id || "";
      const featureComponents = (componentsByFeature.get(featureId) || []) as Component[];
      const filteredGroups = featureComponents.filter((component) => component.ComponentType === "Section");
      filteredGroups.forEach((group) => {
        group.Components = featureComponents.filter((component) => component.ComponentGroupId === group.Id);
      });
      feature.UserSettings = settingsByFeature.get(featureId) || [];
      feature.Components = featureComponents.filter((component) => component.ComponentType === "Button");
      feature.ComponentGroup = filteredGroups;
      feature.FeaturePolicies = policiesByFeature.get(featureId) || [];
      feature.GridPolicies = featureComponents.filter((component) => !component.ComponentGroupId && component.EntityId != null);
      await this.saveFeatureToJson(feature, tenant);
    }
    return true;
  }

  async publishFeatureByName(name: string, tenant?: string | null): Promise<boolean> {
    const conn = this.context.getDefaultConn();
    const features = await this.context.query(
      `select * from [Feature] where Name = ${escapeValue(name, this.context.sqlDialect)}`,
      conn,
    );
    if (isEmpty(features)) return true;
    const featureIds = features.map((row) => toStringSafe(getRowValue(row, "Id")));
    const [components, policies, settings] = await Promise.all([
      this.context.query(
        `select [Component].*, isnull(def.Value, DefaultVal) as DefaultVal, def.Id as ComponentDefaultValueId from [Component] outer apply (select top 1 Value,Id from ComponentDefaultValue where ComponentId = Component.Id) as def where FeatureId in (${combineStrings(featureIds, this.context.sqlDialect)})`,
        conn,
      ),
      this.context.query(`select * from [FeaturePolicy] where FeatureId in (${combineStrings(featureIds, this.context.sqlDialect)})`, conn),
      this.context.query(`select * from [UserSetting] where FeatureId in (${combineStrings(featureIds, this.context.sqlDialect)})`, conn),
    ]);

    const componentsByFeature = new Map<string, Array<Record<string, unknown>>>();
    components.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!componentsByFeature.has(featureId)) componentsByFeature.set(featureId, []);
      componentsByFeature.get(featureId)?.push(row);
    });

    const policiesByFeature = new Map<string, FeaturePolicy[]>();
    policies.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!policiesByFeature.has(featureId)) policiesByFeature.set(featureId, []);
      policiesByFeature.get(featureId)?.push(row as FeaturePolicy);
    });

    const settingsByFeature = new Map<string, Array<Record<string, unknown>>>();
    settings.forEach((row) => {
      const featureId = toStringSafe(getRowValue(row, "FeatureId"));
      if (!settingsByFeature.has(featureId)) settingsByFeature.set(featureId, []);
      settingsByFeature.get(featureId)?.push(row as Record<string, unknown>);
    });

    for (const row of features) {
      const feature = row as Feature;
      const featureId = feature.Id || "";
      const featureComponents = (componentsByFeature.get(featureId) || []) as Component[];
      const filteredGroups = featureComponents.filter((component) => component.ComponentType === "Section");
      filteredGroups.forEach((group) => {
        group.Components = featureComponents.filter((component) => component.ComponentGroupId === group.Id);
      });
      feature.UserSettings = settingsByFeature.get(featureId) || [];
      feature.Components = featureComponents.filter((component) => component.ComponentType === "Button");
      feature.ComponentGroup = filteredGroups;
      feature.FeaturePolicies = policiesByFeature.get(featureId) || [];
      feature.GridPolicies = featureComponents.filter((component) => !component.ComponentGroupId && component.EntityId != null);
      await this.saveFeatureToJson(feature, tenant || this.context.TenantCode || "system");
    }
    return true;
  }

  private async saveFeatureToJson(feature: Feature, tenant: string): Promise<void> {
    const directoryPath = path.join(this.context.metadataRoot, tenant, "features");
    const filePath = path.join(directoryPath, `${feature.Name}.json`);
    await ensureDirectoryExists(filePath);
    const json = JSON.stringify(feature, null, 2);
    await writeText(filePath, json);
  }

  async getFeatureFromJson(featureName: string, tenant: string): Promise<Feature | null> {
    const feature = await this.context.metadataStore.getFeature(featureName, { tenant });
    if (feature) return feature;
    const filePath = path.join(this.context.metadataRoot, tenant, "features", `${featureName}.json`);
    const content = await readText(filePath);
    if (!content) return null;
    return parseJsonSafe<Feature>(content);
  }
}
