import path from "path";
import { readFile } from "fs/promises";
import type { Feature, MetadataStore, RuntimeContext } from "./types.js";
import { isNullOrWhiteSpace } from "./utils.js";

const readJson = async <T>(filePath: string): Promise<T | null> => {
  try {
    if (typeof Bun !== "undefined") {
      const text = await Bun.file(filePath).text();
      return JSON.parse(text) as T;
    }
    const text = await readFile(filePath, "utf-8");
    return JSON.parse(text) as T;
  } catch {
    return null;
  }
};

export class FileMetadataStore implements MetadataStore {
  constructor(private baseDir: string, private tenant: string = "system") {}

  async getFeature(name: string, context?: RuntimeContext): Promise<Feature | null> {
    if (isNullOrWhiteSpace(name)) return null;
    const tenant = (context?.tenant || this.tenant).toLowerCase();
    const featurePath = path.join(this.baseDir, tenant, "features", `${name}.json`);
    return readJson<Feature>(featurePath);
  }

  async getPublicFeature(name: string, context?: RuntimeContext): Promise<Feature | null> {
    return this.getFeature(name, context);
  }
}
