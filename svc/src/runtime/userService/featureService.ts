import path from "node:path";
import type { Component, Feature } from "../types.ts";
import type { UserServiceContext } from "./types.ts";
import { parseJsonSafe } from "../utils.ts";
import { readText, writeText, ensureDirectoryExists } from "./utils.ts";

export class FeatureService {
  constructor(private context: UserServiceContext) {}

  async getFeature(name: string): Promise<Feature> {
    const feature = await this.context.metadataStore.getFeature(name, this.context.getRuntimeContext());
    if (!feature) throw new Error("Feature not found");
    return feature;
  }

  async saveFeatureToJson(feature: Feature, tenant: string): Promise<void> {
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


  async findComponentById(
    comId: string,
    components: Component[],
    roleIds: string[],
    feature?: Feature,
  ): Promise<Component | null> {
    for (const component of components) {
      if (component.Id === comId) {
        if (!component.IsPrivate || roleIds.includes("ADMIN")) {
          return component;
        }
        if (feature) {
          const permissions = (feature.FeaturePolicies || []).filter(
            (policy) => roleIds.includes(policy.RoleId || "") && policy.CanRead,
          );
          return permissions.length > 0 ? component : null;
        }
        return null;
      }
      if (component.Components && component.Components.length > 0) {
        const found = await this.findComponentById(comId, component.Components, roleIds, feature);
        if (found) return found;
      }
    }
    return null;
  }
}
