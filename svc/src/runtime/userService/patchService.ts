import type {
  Component,
  PatchDetail,
  PatchVM,
  SqlViewModel,
} from "../types.js";
import type { UserServiceContext } from "./types.js";
import type { FeatureService } from "./featureService.js";
import { parseJsonSafe } from "../utils.js";
import {
  combineStrings,
  escapeValue,
  formatEntity,
  getChangeValue,
  getRowValue,
  isEmpty,
  toStringSafe,
} from "./utils.js";

export class PatchService {
  constructor(private context: UserServiceContext, private featureService: FeatureService) {}

  addDefaultFields(changes: PatchDetail[], defaults: PatchDetail[]): void {
    defaults.forEach((field) => {
      const existing = changes.find((change) => change.Field === field.Field);
      if (existing) {
        existing.Value = field.Value;
      } else {
        changes.push(field);
      }
    });
  }

  async savePatch(vm: PatchVM): Promise<number> {
    const sql = this.context.sqlBuilder.buildCreateOrUpdate(vm);
    if (!sql) return 0;
    return this.context.execute(sql);
  }

  async updatePatch(vm: PatchVM): Promise<number> {
    const sql = this.context.sqlBuilder.buildUpdate(vm);
    if (!sql) return 0;
    return this.context.execute(sql);
  }

  async hardDelete(vm: PatchVM): Promise<boolean> {
    if (!vm.ComId) {
      const sql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      if (sql) await this.context.execute(sql);
      return true;
    }
    const componentRows = await this.context.query(`select top 1 * from [Component] where Id = ${escapeValue(vm.ComId, this.context.sqlDialect)}`);
    const com = componentRows[0] as Component | undefined;
    if (!com || !com.Query) return false;
    const data = parseJsonSafe<{ update?: string }>(com.Query);
    const dictionary: Record<string, unknown> = {
      EntityIds: combineStrings(vm.Delete?.flatMap((item) => item.Ids) || [], this.context.sqlDialect),
      NewId: vm.NewId,
    };
    if (data?.update) {
      const updateSql = formatEntity(data.update, dictionary);
      const deleteSql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      await this.context.execute([updateSql, deleteSql].filter(Boolean).join(";"));
    } else {
      const deleteSql = (vm.Delete || []).map((item) => `delete from [${item.Table}] where Id in (${combineStrings(item.Ids, this.context.sqlDialect)})`).join(";");
      await this.context.execute(deleteSql);
    }
    return true;
  }

  async savePatches(patches: PatchVM[]): Promise<number> {
    if (isEmpty(patches)) throw new Error("patches is null or empty");
    const usable = patches.filter((patch) => getChangeValue(patch, "Id") != null);
    if (isEmpty(usable)) throw new Error("patches is null or empty");
    const sql = usable
      .map((patch) => this.context.sqlBuilder.buildCreateOrUpdate(patch))
      .filter((statement) => statement && statement.trim() !== "")
      .join(";\n");
    return this.context.execute(sql);
  }

  async deactivateAsync(vm: SqlViewModel): Promise<string[] | null> {
    vm.CachedDataConn = await this.context.resolveConnection(vm.DataConn || "default") || vm.CachedDataConn;
    const rows = await this.context.query(`select * from [${vm.Table}] where Id in (${combineStrings(vm.Id, this.context.sqlDialect)})`);
    if (isEmpty(rows)) return null;
    const rowIds = rows.map((row) => toStringSafe(getRowValue(row, "Id")));
    await this.context.execute(`update ${vm.Table} set Active = 0 where Id in (${combineStrings(rowIds, this.context.sqlDialect)})`);
    return rowIds;
  }
}
