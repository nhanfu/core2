/**
 * Patch Service - CRUD Operations
 * Handles create, update, delete, and deactivate operations for database records
 * Migration from CoreAPI PatchService.cs
 */

import { query, execute, insert, update, remove } from "../database/postgresClient.ts";
import type { PatchVM, PatchDetail, FeaturePolicy, SqlResult, SqlViewModel, UserContext } from "../types/interfaces.ts";

/**
 * PatchService - Handles CRUD operations with permission checking
 */
export class PatchService {
  private userContext: UserContext | null = null;
  private cache: Map<string, { data: any; expiry: number }> = new Map();
  private readonly CACHE_TTL = 5 * 60 * 1000; // 5 minutes

  /**
   * Set the user context for the current request
   */
  setUserContext(context: UserContext): void {
    this.userContext = context;
  }

  /**
   * Get current user ID
   */
  private get userId(): string {
    return this.userContext?.userId || "";
  }

  /**
   * Get current tenant code
   */
  private get tenantCode(): string {
    return this.userContext?.tenantCode || "system";
  }

  /**
   * Get current environment
   */
  private get env(): string {
    return this.userContext?.env || "prod";
  }

  /**
   * Get current role IDs
   */
  private get roleIds(): string[] {
    return this.userContext?.roles || [];
  }

  /**
   * Cache helper - get value
   */
  private async getCache(key: string): Promise<any | null> {
    const cached = this.cache.get(key);
    if (cached && cached.expiry > Date.now()) {
      return cached.data;
    }
    return null;
  }

  /**
   * Cache helper - set value
   */
  private async setCache(key: string, value: any, ttl: number = this.CACHE_TTL): Promise<void> {
    this.cache.set(key, { data: value, expiry: Date.now() + ttl });
  }

  /**
   * SavePatch - Create or update a single record
   * @param patchVM - The patch view model containing table, changes, and metadata
   * @returns Number of affected rows
   */
  async SavePatch(patchVM: PatchVM): Promise<number> {
    // Check write permission
    const canWrite = await this.hasWritePermission(patchVM);
    if (!canWrite) {
      throw new Error(`Unauthorized to write on "${patchVM.table}"`);
    }

    const idField = patchVM.changes?.find((c: PatchDetail) => c.field.toLowerCase() === "id");
    const id = idField?.value || "";
    const isNew = !id || id.startsWith("-");

    if (isNew) {
      return await this.insertRecord(patchVM);
    } else {
      return await this.updateRecord(patchVM);
    }
  }

  /**
   * SavePatch2 - Alternative save with explicit ID handling
   * Returns SqlResult with status and data
   * @param patchVM - The patch view model
   * @returns SqlResult with operation status
   */
  async SavePatch2(patchVM: PatchVM): Promise<SqlResult> {
    const table = patchVM.table;
    if (!table) {
      return { message: "Table is required", status: 400 };
    }

    const idField = patchVM.changes?.find((c: PatchDetail) => c.field.toLowerCase() === "id");
    const id = idField?.value;

    if (!id) {
      return { message: "Id is required", status: 400 };
    }

    // Check write permission
    const canWrite = await this.hasWritePermission(patchVM);
    if (!canWrite) {
      return { message: `Unauthorized to write on "${table}"`, status: 401 };
    }

    if (id.startsWith("-")) {
      // Insert new record
      return await this.insertRecordWithResult(patchVM, id.substring(1));
    } else {
      // Update existing record
      return await this.updateRecordWithResult(patchVM, id);
    }
  }

  /**
   * SavePatchs - Batch save multiple records
   * @param patchVMs - Array of patch view models
   * @returns SqlResult with aggregated results
   */
  async SavePatchs(patchVMs: PatchVM[]): Promise<SqlResult> {
    if (!patchVMs || patchVMs.length === 0) {
      return { message: "No patches to save", status: 400 };
    }

    const results: SqlResult[] = [];
    let successCount = 0;

    for (const patchVM of patchVMs) {
      const result = await this.SavePatch2(patchVM);
      results.push(result);
      if (result.status === 200) {
        successCount++;
      } else {
        // Stop on first error or continue based on requirements
        break;
      }
    }

    return {
      message: `${successCount}/${patchVMs.length} operations successful`,
      status: successCount === patchVMs.length ? 200 : 207,
      data: results.map(r => r.updatedItem || []).flat(),
    };
  }

  /**
   * HardDelete - Permanently delete records
   * @param table - The table name
   * @param id - The record ID(s) to delete
   * @returns Boolean indicating success
   */
  async HardDelete(table: string, id: string | string[]): Promise<boolean> {
    if (!table) {
      throw new Error("Table name is required");
    }

    const ids = Array.isArray(id) ? id : [id];
    if (ids.length === 0) {
      return true;
    }

    // Check delete permission
    const canDelete = await this.hasDeletePermission(table, ids);
    if (!canDelete) {
      throw new Error(`Unauthorized to delete from "${table}"`);
    }

    try {
      const idsString = ids.map((i: string) => `'${i}'`).join(",");
      const sql = `DELETE FROM "${table}" WHERE "Id" IN (${idsString})`;
      await execute(sql);
      return true;
    } catch (error) {
      console.error("HardDelete error:", error);
      return false;
    }
  }

  /**
   * DeactivateAsync - Soft delete (set Active = false)
   * @param table - The table name
   * @param id - The record ID(s) to deactivate
   * @returns Array of unauthorized IDs (empty if all succeeded)
   */
  async DeactivateAsync(table: string, id: string | string[]): Promise<string[]> {
    if (!table) {
      throw new Error("Table name is required");
    }

    const ids = Array.isArray(id) ? id : [id];
    if (ids.length === 0) {
      return [];
    }

    // Check deactivate permission
    const permission = await this.getEntityPermission(table);
    const canDeactivateAll = permission.some((p: FeaturePolicy) => p.canDeleteAll);
    const canDeactivateSelf = permission.some((p: FeaturePolicy) => p.canDeactivate);

    // Get current records to check ownership
    const idsString = ids.map((i: string) => `'${i}'`).join(",");
    const selectSql = `SELECT * FROM "${table}" WHERE "Id" IN (${idsString})`;
    const records = await query(selectSql);

    if (!records || records.length === 0) {
      return [];
    }

    // Check authorization for each record
    const unauthorized: string[] = [];
    for (const record of records) {
      const isOwner = this.isOwner(record, this.userId, this.roleIds);
      if (!canDeactivateAll && !(canDeactivateSelf && isOwner)) {
        unauthorized.push(record.Id || record.id || "");
      }
    }

    if (unauthorized.length > 0) {
      return unauthorized;
    }

    // Perform soft delete
    const activeField = records[0].hasOwnProperty("Active") ? "Active" :
                        records[0].hasOwnProperty("IsActive") ? "IsActive" : "active";
    const updateSql = `UPDATE "${table}" SET "${activeField}" = false WHERE "Id" IN (${idsString})`;
    await execute(updateSql);

    return [];
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  /**
   * Insert a new record
   */
  private async insertRecord(patchVM: PatchVM): Promise<number> {
    const table = patchVM.table;
    if (!table) throw new Error("Table is required");

    // Add default fields for new records
    this.addDefaultFields(patchVM.changes || [], [
      { field: "InsertedDate", value: new Date().toISOString() },
      { field: "InsertedBy", value: this.userId },
      { field: "UpdatedDate", value: null },
      { field: "UpdatedBy", value: null },
      { field: "Active", value: "true" },
    ]);

    const data = this.changesToObject(patchVM.changes || []);
    const result = await insert(table, data);
    return result?.length || 0;
  }

  /**
   * Insert record and return SqlResult
   */
  private async insertRecordWithResult(patchVM: PatchVM, id: string): Promise<SqlResult> {
    const table = patchVM.table as string;

    // Add default fields
    this.addDefaultFields(patchVM.changes || [], [
      { field: "Id", value: id },
      { field: "InsertedDate", value: new Date().toISOString() },
      { field: "InsertedBy", value: this.userId },
      { field: "UpdatedDate", value: null },
      { field: "UpdatedBy", value: null },
      { field: "Active", value: "true" },
    ]);

    const data = this.changesToObject(patchVM.changes || []);

    try {
      const result = await insert(table, data);
      if (result && result.length > 0) {
        // Fetch the inserted record
        const selectResult = await query(`SELECT * FROM "${table}" WHERE "Id" = '${id}'`);
        return {
          message: "create successful",
          status: 200,
          updatedItem: selectResult,
        };
      }
      return { message: "Insert failed", status: 500 };
    } catch (error: any) {
      console.error("Insert error:", error);
      return { message: error.message || "Insert failed", status: 500 };
    }
  }

  /**
   * Update an existing record
   */
  private async updateRecord(patchVM: PatchVM): Promise<number> {
    const table = patchVM.table as string;
    if (!table) throw new Error("Table is required");

    const idField = patchVM.changes?.find((c: PatchDetail) => c.field.toLowerCase() === "id");
    const id = idField?.value;
    if (!id) throw new Error("ID is required for update");

    // Add default fields for updates
    this.addDefaultFields(patchVM.changes || [], [
      { field: "UpdatedDate", value: new Date().toISOString() },
      { field: "UpdatedBy", value: this.userId },
    ]);

    const data = this.changesToObject(patchVM.changes || []);
    // Remove Id from update data
    delete data.Id;
    delete data.id;

    const result = await update(table, data, { Id: id });
    return result?.length || 0;
  }

  /**
   * Update record and return SqlResult
   */
  private async updateRecordWithResult(patchVM: PatchVM, id: string): Promise<SqlResult> {
    const table = patchVM.table as string;

    // Add default fields
    this.addDefaultFields(patchVM.changes || [], [
      { field: "UpdatedDate", value: new Date().toISOString() },
      { field: "UpdatedBy", value: this.userId },
    ]);

    const data = this.changesToObject(patchVM.changes || []);
    // Remove Id from update data
    delete data.Id;
    delete data.id;

    try {
      const result = await update(table, data, { Id: id });
      if (result && result.length > 0) {
        // Fetch the updated record
        const selectResult = await query(`SELECT * FROM "${table}" WHERE "Id" = '${id}'`);
        return {
          message: "update successful",
          status: 200,
          updatedItem: selectResult,
        };
      }
      return { message: "Update failed", status: 500 };
    } catch (error: any) {
      console.error("Update error:", error);
      return { message: error.message || "Update failed", status: 500 };
    }
  }

  /**
   * Check if user has write permission for the table
   */
  private async hasWritePermission(patchVM: PatchVM): Promise<boolean> {
    // Bypass permission check if flag is set
    if (patchVM.byPassPerm) return true;

    const table = patchVM.table;
    if (!table) return false;

    const idField = patchVM.changes?.find((c: PatchDetail) => c.field.toLowerCase() === "id");
    const oldId = idField?.oldVal;

    const permissions = await this.getEntityPermission(table);

    if (!oldId) {
      // New record - check CanWriteAll
      return permissions.some((p: FeaturePolicy) => p.canWriteAll);
    } else {
      // Existing record - check ownership or CanWriteAll
      const recordQuery = `SELECT * FROM "${table}" WHERE "Id" = '${oldId}'`;
      const records = await query(recordQuery);
      const record = records?.[0];
      const isOwner = this.isOwner(record, this.userId, this.roleIds);
      return isOwner || permissions.some((p: FeaturePolicy) => p.canWriteAll);
    }
  }

  /**
   * Check if user has delete permission for the table
   */
  private async hasDeletePermission(table: string, ids: string[]): Promise<boolean> {
    if (!table || ids.length === 0) return false;

    const permissions = await this.getEntityPermission(table);

    // Check CanDeleteAll
    if (permissions.some((p: FeaturePolicy) => p.canDeleteAll)) {
      return true;
    }

    // Check CanDelete with ownership
    const canDeleteSelf = permissions.some((p: FeaturePolicy) => p.canDelete);
    if (!canDeleteSelf) return false;

    // Check ownership for all records
    const idsString = ids.map((i: string) => `'${i}'`).join(",");
    const records = await query(`SELECT * FROM "${table}" WHERE "Id" IN (${idsString})`);

    for (const record of records) {
      if (!this.isOwner(record, this.userId, this.roleIds)) {
        return false;
      }
    }

    return true;
  }

  /**
   * Get entity permissions from FeaturePolicy table
   */
  private async getEntityPermission(entityName: string, recordId?: string): Promise<FeaturePolicy[]> {
    if (!entityName || this.roleIds.length === 0) {
      return [];
    }

    const cacheKey = `${entityName}_permissions`;
    const cached = await this.getCache(cacheKey);
    if (cached) {
      return cached;
    }

    const roleIdsString = this.roleIds.map((r: string) => `'${r}'`).join(",");
    const sql = `
      SELECT * FROM "FeaturePolicy"
      WHERE "Active" = true
      AND "EntityName" = '${entityName}'
      AND ("RecordId" = '${recordId || ''}' OR '${recordId || ''}' = '')
      AND "RoleId" IN (${roleIdsString})
    `;

    try {
      const permissions = await query(sql);
      await this.setCache(cacheKey, permissions);
      return permissions;
    } catch (error) {
      console.error("Error fetching permissions:", error);
      return [];
    }
  }

  /**
   * Check if user is owner of a record
   * Owner = InsertedBy == UserId or user has ADMIN role
   */
  private isOwner(record: any, userId: string, roleIds: string[]): boolean {
    if (!record) return false;

    // Check if user has ADMIN role
    if (roleIds.includes("ADMIN") || roleIds.includes("admin")) {
      return true;
    }

    const insertedBy = record.InsertedBy || record.insertedBy || record.InsertedBy_userId;
    return insertedBy === userId;
  }

  /**
   * Add default fields to changes array
   */
  private addDefaultFields(changes: PatchDetail[], defaultFields: { field: string; value: any }[]): void {
    for (const field of defaultFields) {
      const existing = changes.find((c: PatchDetail) => c.field.toLowerCase() === field.field.toLowerCase());
      if (existing) {
        existing.value = field.value;
      } else {
        changes.push({
          field: field.field,
          value: field.value,
        });
      }
    }
  }

  /**
   * Convert PatchDetail array to object for database insert/update
   */
  private changesToObject(changes: PatchDetail[]): Record<string, any> {
    const obj: Record<string, any> = {};
    for (const change of changes) {
      if (change.field.toLowerCase() === "id") continue; // Skip Id field for data object
      obj[change.field] = this.parseValue(change.value);
    }
    return obj;
  }

  /**
   * Parse value to appropriate type
   */
  private parseValue(value: any): any {
    if (value === null || value === undefined) {
      return null;
    }
    if (value === "true" || value === "false") {
      return value === "true";
    }
    // Try parsing as number
    const num = Number(value);
    if (!isNaN(num) && value !== "") {
      return num;
    }
    return value;
  }
}

// Export singleton instance
export const patchService = new PatchService();
