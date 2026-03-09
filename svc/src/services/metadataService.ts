/**
 * Metadata Service
 * Loads feature metadata from YAML/JSON files
 * Similar to CoreAPI's MetadataService.cs
 */

import { parse as parseYaml } from "@std/yaml";
import { join } from "@std/path";
import { existsSync } from "@std/fs";
import type { Feature } from "../types/interfaces.ts";

/**
 * Feature metadata storage path
 */
const FEATURES_FOLDER = "wwwroot/upload";

/**
 * Default tenant code fallback
 */
const DEFAULT_TENANT = "crm";

/**
 * MetadataService class for loading feature metadata
 */
export class MetadataService {
  private tenantCode: string = DEFAULT_TENANT;
  private roleIds: string[] = [];

  constructor() {}

  /**
   * Set user context for metadata operations
   * @param tenantCode - The tenant code
   * @param roleIds - List of role IDs for permission filtering
   */
  setUserContext(tenantCode: string, roleIds: string[]): void {
    this.tenantCode = tenantCode || DEFAULT_TENANT;
    this.roleIds = roleIds || [];
  }

  /**
   * Get feature folder path for a tenant
   * @param tenantCode - The tenant code
   * @returns Full path to the features folder
   */
  private getFeatureFolderPath(tenantCode: string): string {
    const normalized = (tenantCode || DEFAULT_TENANT).trim().toLowerCase();
    const candidates = [normalized, normalized.toLowerCase(), DEFAULT_TENANT];

    // Try to find existing tenant folder
    for (const candidate of [...new Set(candidates)]) {
      if (!candidate) continue;
      const candidatePath = join(Deno.cwd(), FEATURES_FOLDER, candidate, "features");
      if (existsSync(candidatePath)) {
        return candidatePath;
      }
    }

    // Check if upload root exists
    const uploadRoot = join(Deno.cwd(), FEATURES_FOLDER);
    if (existsSync(uploadRoot)) {
      for (const entry of Deno.readDirSync(uploadRoot)) {
        if (entry.isDirectory && entry.name.toLowerCase() === normalized.toLowerCase()) {
          return join(uploadRoot, entry.name, "features");
        }
      }
    }

    // Return default path
    return join(Deno.cwd(), FEATURES_FOLDER, normalized.toLowerCase(), "features");
  }

  /**
   * Resolve tenant folder with fallback
   * @param uploadRoot - Root upload folder
   * @param tenantCode - Tenant code to resolve
   * @param createIfMissing - Whether to create folder if missing
   * @returns Resolved tenant folder path
   */
  private resolveTenantFolder(
    uploadRoot: string,
    tenantCode: string,
    createIfMissing: boolean = false,
  ): string {
    const normalized = (tenantCode || DEFAULT_TENANT).trim();
    const candidates = [normalized, normalized.toLowerCase(), DEFAULT_TENANT];

    for (const candidate of [...new Set(candidates)].filter((x) => x)) {
      const candidatePath = join(uploadRoot, candidate);
      if (existsSync(candidatePath)) {
        return candidatePath;
      }
    }

    if (existsSync(uploadRoot)) {
      for (const entry of Deno.readDirSync(uploadRoot)) {
        if (entry.isDirectory && entry.name.toLowerCase() === normalized.toLowerCase()) {
          return join(uploadRoot, entry.name);
        }
      }
    }

    return createIfMissing
      ? join(uploadRoot, normalized.toLowerCase())
      : join(uploadRoot, DEFAULT_TENANT);
  }

  /**
   * Get menu items for user roles
   * @param tenantCode - The tenant code
   * @param roleIds - List of role IDs
   * @returns Array of menu items as dictionaries
   */
  async GetMenu(tenantCode: string, roleIds: string[]): Promise<Record<string, unknown>[]> {
    try {
      const effectiveTenant = tenantCode || this.tenantCode || DEFAULT_TENANT;
      const effectiveRoles = roleIds || this.roleIds;
      const basePath = this.getFeatureFolderPath(effectiveTenant);

      console.log(`GetMenu called - TenantCode: ${effectiveTenant}, BasePath: ${basePath}`);

      if (!existsSync(basePath)) {
        console.warn(`Directory does not exist: ${basePath}`);
        return [];
      }

      const menuItems: Record<string, unknown>[] = [];

      // Read both YAML and JSON files
      const dirEntries = Array.from(Deno.readDirSync(basePath));
      const yamlFiles = dirEntries
        .filter((f) => f.isFile && f.name.endsWith(".yaml"))
        .map((f) => join(basePath, f.name));
      const jsonFiles = dirEntries
        .filter((f) => f.isFile && f.name.endsWith(".json"))
        .map((f) => join(basePath, f.name));

      const allFiles = [...yamlFiles, ...jsonFiles];

      for (const file of allFiles) {
        try {
          const feature = this.ReadFeatureFile(file);
          if (feature?.isMenu && this.hasMenuPermission(feature, effectiveRoles)) {
            menuItems.push(this.convertFeatureToDictionary(feature));
          }
        } catch (ex) {
          console.warn(`Error reading feature file: ${file}`, ex);
        }
      }

      // Sort by Order
      const result = menuItems.sort((a, b) =>
        (a["Order"] as number ?? 999) - (b["Order"] as number ?? 999)
      );

      console.log(`Returning ${result.length} menu items`);
      return result;
    } catch (ex) {
      console.error("Error in GetMenu", ex);
      return [];
    }
  }

  /**
   * Load a specific feature by ID/name
   * @param tenantCode - The tenant code
   * @param featureId - The feature ID or name
   * @returns The feature object
   * @throws Error if feature not found
   */
  async LoadFeature(tenantCode: string, featureId: string): Promise<Feature> {
    const effectiveTenant = tenantCode || this.tenantCode || DEFAULT_TENANT;
    const feature = this.loadFeatureFromFile(featureId, effectiveTenant);

    if (!feature) {
      const error = new Error("Feature not found") as Error & { statusCode: number };
      error.statusCode = 404;
      throw error;
    }

    return feature;
  }

  /**
   * Load all features for a tenant
   * @param tenantCode - The tenant code
   * @returns Array of all features
   */
  async LoadAllFeatures(tenantCode: string): Promise<Feature[]> {
    const effectiveTenant = tenantCode || this.tenantCode || DEFAULT_TENANT;
    const basePath = this.getFeatureFolderPath(effectiveTenant);

    if (!existsSync(basePath)) {
      console.warn(`Directory does not exist: ${basePath}`);
      return [];
    }

    const features: Feature[] = [];

    // Read both YAML and JSON files
    const dirEntries = Array.from(Deno.readDirSync(basePath));
    const yamlFiles = dirEntries
      .filter((f) => f.isFile && f.name.endsWith(".yaml"))
      .map((f) => join(basePath, f.name));
    const jsonFiles = dirEntries
      .filter((f) => f.isFile && f.name.endsWith(".json"))
      .map((f) => join(basePath, f.name));

    const allFiles = [...yamlFiles, ...jsonFiles];

    for (const file of allFiles) {
      try {
        const feature = this.ReadFeatureFile(file);
        if (feature) {
          features.push(feature);
        }
      } catch (ex) {
        console.warn(`Error reading feature file: ${file}`, ex);
      }
    }

    return features;
  }

  /**
   * Read and parse a feature file (YAML or JSON)
   * @param filePath - Full path to the feature file
   * @returns Parsed Feature object
   */
  ReadFeatureFile(filePath: string): Feature | null {
    if (!existsSync(filePath)) {
      console.warn(`File does not exist: ${filePath}`);
      return null;
    }

    const content = Deno.readTextFileSync(filePath);
    const ext = filePath.toLowerCase().endsWith(".yaml") ? ".yaml" : ".json";

    try {
      if (ext === ".yaml") {
        return parseYaml(content) as Feature;
      } else {
        return JSON.parse(content) as Feature;
      }
    } catch (ex) {
      console.error(`Error parsing feature file: ${filePath}`, ex);
      return null;
    }
  }

  /**
   * Load feature from file by name
   * @param featureName - Feature name (without extension)
   * @param tenantCode - Tenant code
   * @returns Feature object or null if not found
   */
  private loadFeatureFromFile(featureName: string, tenantCode: string): Feature | null {
    const basePath = this.getFeatureFolderPath(tenantCode);

    // Try YAML first
    const yamlPath = join(basePath, `${featureName}.yaml`);
    if (existsSync(yamlPath)) {
      return this.ReadFeatureFile(yamlPath);
    }

    // Try JSON
    const jsonPath = join(basePath, `${featureName}.json`);
    if (existsSync(jsonPath)) {
      return this.ReadFeatureFile(jsonPath);
    }

    return null;
  }

  /**
   * Check if user has permission to view menu item
   * @param feature - The feature to check
   * @param roleIds - User's role IDs
   * @returns True if user has permission
   */
  private hasMenuPermission(feature: Feature, roleIds: string[]): boolean {
    // Admin has full access
    if (roleIds.includes("ADMIN")) {
      return true;
    }

    // Check feature policies
    if (feature.featurePolicies && feature.featurePolicies.length > 0) {
      return feature.featurePolicies.some(
        (p) => roleIds.includes(p.roleId) && p.canRead === true,
      );
    }

    // If no policies defined, allow access
    return true;
  }

  /**
   * Convert Feature to dictionary for API response
   * @param feature - The feature to convert
   * @returns Dictionary representation
   */
  private convertFeatureToDictionary(feature: Feature): Record<string, unknown> {
    return {
      Id: feature.id,
      Name: feature.name,
      Label: feature.label || feature.name || "",
      Order: feature.order ?? 999,
      Icon: feature.icon || "",
      IsMenu: feature.isMenu,
      ParentId: feature.parentId || "",
      ClassName: feature.className || "",
      Style: feature.style || "",
      Script: feature.script || "",
      Events: feature.events || "",
      EntityId: feature.entityId || "",
      Active: feature.active ?? true,
      IsLock: feature.isLock,
      IgnoreEncode: feature.ignoreEncode,
    };
  }

  /**
   * Save feature to JSON file
   * @param feature - Feature to save
   * @param tenantCode - Tenant code
   */
  async SaveFeatureToJson(feature: Feature, tenantCode: string): Promise<void> {
    const effectiveTenant = tenantCode || this.tenantCode || DEFAULT_TENANT;
    const uploadRoot = join(Deno.cwd(), FEATURES_FOLDER);
    const tenantFolder = this.resolveTenantFolder(uploadRoot, effectiveTenant, true);
    const featuresFolder = join(tenantFolder, "features");

    // Create directory if not exists
    if (!existsSync(featuresFolder)) {
      Deno.mkdirSync(featuresFolder, { recursive: true });
    }

    const filePath = join(featuresFolder, `${feature.name}.json`);
    const json = JSON.stringify(feature, null, 2);

    await Deno.writeTextFile(filePath, json);
    console.log(`Feature saved to: ${filePath}`);
  }
}

// Export singleton instance
export const metadataService = new MetadataService();
