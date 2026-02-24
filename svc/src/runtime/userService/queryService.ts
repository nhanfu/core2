import type {
  Component,
  SqlComResult,
  SqlViewModel,
} from "../types.js";
import type { UserServiceContext } from "./types.js";
import type { FeatureService } from "./featureService.js";
import { getRowValue, parseWhereParams } from "./utils.js";
import { parseJsonSafe } from "../utils.js";

export class QueryService {
  constructor(
    private context: UserServiceContext,
    private featureService: FeatureService,
  ) {}

  async comQuery(vm: SqlViewModel): Promise<SqlComResult> {
    const com = await this.getComponent(vm);
    if (!com) throw new Error("Component not found or not public to the current user");
    const invalid = ["select ", "from ", "where ", "group by ", "having ", "order by "]
      .some((term) => [vm.Select, vm.Table, vm.GroupBy, vm.Having, vm.OrderBy, vm.Paging].some((value) => value && value.toLowerCase().includes(term.trim())));
    if (invalid) throw new Error("Parameters must NOT contains sql keywords");
    vm.JsScript = com.Query;
    return this.runjsWrap(vm);
  }

  async readDs(query: string): Promise<Array<Array<Record<string, unknown>>>> {
    return this.context.queryMany(query);
  }

  private async getComponent(vm: SqlViewModel): Promise<Component | null> {
    const comKey = `Component${vm.ComId}`.toUpperCase();
    const cached = await this.context.cache.get(comKey);
    if (cached) {
      const parsed = parseJsonSafe<Component>(cached);
      if (parsed) return parsed;
    }
    const feature = await this.featureService.getFeatureFromJson(vm.Feature || "", this.context.TenantCode || "system");
    if (!feature) return null;
    const com = await this.featureService.findComponentById(
      vm.ComId || "",
      feature.Components || [],
      this.context.RoleIds || [],
      feature,
    );
    if (!com) return null;
    await this.context.cache.set(comKey, JSON.stringify(com));
    return com;
  }

  private async runjsWrap(vm: SqlViewModel): Promise<SqlComResult> {
    const script = new Function(vm.JsScript ?? "");
    const actQuery = script(vm);
    const params = parseWhereParams(vm.WhereParams);
    const data = await this.context.queryMany(actQuery, params);
    const countRow = data.length > 1 && data[1].length > 0 ? data[1][0] : null;
    const countValue = countRow ? Number(getRowValue(countRow, "total")) : null;
    return { count: countValue, value: data[0] || [] };
  }
}
