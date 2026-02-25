import path from "node:path";
import { readFile } from "node:fs/promises";
import { parse as parseYaml } from "yaml";
import type { Feature, MetadataStore, RuntimeContext } from "./types.ts";
import { isNullOrWhiteSpace } from "./utils.ts";

const readText = async (filePath: string): Promise<string | null> => {
  try {
    // Use Deno's readFile if available (Deno runtime), otherwise fallback to Node.js
    if (typeof Deno !== "undefined") {
      return await Deno.readTextFile(filePath);
    }
    return await readFile(filePath, "utf-8");
  } catch {
    return null;
  }
};

const parseMetadata = <T>(content: string, extension: string): T | null => {
  try {
    if (extension === ".yml" || extension === ".yaml") {
      return parseYaml(content) as T;
    }
    return JSON.parse(content) as T;
  } catch {
    return null;
  }
};

const readMetadataFile = async <T>(filePath: string): Promise<T | null> => {
  const content = await readText(filePath);
  if (content === null) return null;
  const extension = path.extname(filePath).toLowerCase();
  return parseMetadata<T>(content, extension);
};

export class FileMetadataStore implements MetadataStore {
  constructor(private baseDir: string, private tenant: string = "system") {}

  async getFeature(name: string, context?: RuntimeContext): Promise<Feature | null> {
    if (isNullOrWhiteSpace(name)) return null;
    const tenant = (context?.tenant || this.tenant).toLowerCase();
    const basePath = path.join(this.baseDir, tenant, "features", name);
    const jsonPath = `${basePath}.json`;
    const yamlPath = `${basePath}.yaml`;
    const ymlPath = `${basePath}.yml`;
    const json = await readMetadataFile<Feature>(jsonPath);
    if (json) return json;
    const yaml = await readMetadataFile<Feature>(yamlPath);
    if (yaml) return yaml;
    return readMetadataFile<Feature>(ymlPath);
  }

  async getPublicFeature(name: string, context?: RuntimeContext): Promise<Feature | null> {
    return this.getFeature(name, context);
  }
}
