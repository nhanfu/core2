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
}
