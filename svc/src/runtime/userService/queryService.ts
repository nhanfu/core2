import type {
  CheckDeleteItem,
  CheckDeleteResult,
  Component,
  Conversation,
  Feature,
  Gos,
  PatchVM,
  SqlComResult,
  SqlQuery,
  SqlResult,
  SqlViewModel,
} from "../types.js";
import type { UserServiceContext } from "./types.js";
import type { FeatureService } from "./featureService.js";
import type { PatchService } from "./patchService.js";
import { combineStrings, escapeValue, formatEntity, getChangeValue, getRowValue, isEmpty, parseWhereParams, toIso, toStringSafe } from "./utils.js";
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

  async sql(vm: SqlViewModel): Promise<Array<Array<Record<string, unknown>>>> {
    const com = await this.getComponent(vm);
    if (!com) throw new Error("Component not found or not public to the current user");
    const dictionary = vm.Params ? (parseJsonSafe<Record<string, unknown>>(vm.Params) || {}) : {};
    dictionary.TokenUserId = this.context.UserId || "";
    dictionary.TokenRoleNames = (this.context.RoleNames || []).join(",");
    dictionary.TokenPartnerId = this.context.VendorId || "";
    dictionary.TokenGroupId = this.context.GroupId || "";
    dictionary.TokenUserName = this.context.UserName || "";
    if (com.Query && com.Query.includes("ds.InsertedBy = '{TokenUserId}'") && (this.context.RoleNames || []).includes("BOD")) {
      com.Query = com.Query.replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
    }
    const query = formatEntity(com.Query || "", dictionary);
    return this.context.queryMany(query);
  }

  async readDs(query: string): Promise<Array<Array<Record<string, unknown>>>> {
    return this.context.queryMany(query);
  }

  private calcFinalQuery(vm: SqlViewModel): string {
    const dictionary = vm.Params ? (parseJsonSafe<Record<string, unknown>>(vm.Params) || {}) : {};
    dictionary.TokenUserId = this.context.UserId || "";
    dictionary.TokenRoleNames = (this.context.RoleNames || []).join(",");
    dictionary.TokenPartnerId = this.context.VendorId || "";
    dictionary.TokenUserName = this.context.UserName || "";
    dictionary.TokenGroupId = this.context.GroupId || "";
    if (vm.JsScript && vm.JsScript.includes("ds.InsertedBy = '{TokenUserId}'") && (this.context.RoleNames || []).includes("BOD")) {
      vm.JsScript = vm.JsScript.replace("ds.InsertedBy = '{TokenUserId}'", "ds.InsertedBy = '{TokenUserId}' or '{TokenRoleNames}' like '%BOD%'");
    }
    vm.OrderBy = formatEntity(vm.OrderBy || "", dictionary);
    const data = parseJsonSafe<SqlQuery>(vm.JsScript || "") || {};
    dictionary.Skip = vm.Skip;
    dictionary.Top = vm.Top;
    if (data.total) data.total = formatEntity(data.total, dictionary);
    if (data.sql) data.sql = formatEntity(data.sql, dictionary);
    let sqlSelect = data.sql || "";
    let sqlTotal = data.total || "";
    if (vm.Where && vm.Where.trim() !== "") {
      if (sqlSelect.toLowerCase().includes("where")) {
        sqlSelect += ` AND (${vm.Where})`;
        sqlTotal += ` AND (${vm.Where})`;
      } else {
        sqlSelect += ` WHERE ${vm.Where}`;
        sqlTotal += ` WHERE ${vm.Where}`;
      }
    }
    if (vm.OrderBy && !sqlSelect.toLowerCase().includes("order by")) {
      sqlSelect += ` ORDER BY ${vm.OrderBy}`;
    }
    if (vm.Skip != null && !sqlSelect.toLowerCase().includes("offset")) {
      sqlSelect += ` OFFSET ${vm.Skip} ROWS`;
    }
    if (vm.Top != null && !sqlSelect.toLowerCase().includes("fetch next")) {
      sqlSelect += ` FETCH NEXT ${vm.Top} ROWS ONLY`;
    }
    if (vm.Count) {
      sqlSelect += `; ${sqlTotal}`;
    }
    return sqlSelect;
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
    const com = await this.findComponentById(vm, feature.Components || [], feature);
    if (!com) return null;
    await this.context.cache.set(comKey, JSON.stringify(com));
    return com;
  }

  private async findComponentById(vm: SqlViewModel, components: Component[], feature: Feature): Promise<Component | null> {
    for (const component of components) {
      if (component.Id === vm.ComId) {
        if (!component.IsPrivate || (this.context.RoleIds || []).includes("ADMIN")) {
          return component;
        }
        const permissions = (feature.FeaturePolicies || []).filter(
          (policy) => (this.context.RoleIds || []).includes(policy.RoleId || "") && policy.CanRead,
        );
        return permissions.length > 0 ? component : null;
      }
      if (component.Components && component.Components.length > 0) {
        const found = await this.findComponentById(vm, component.Components, feature);
        if (found) return found;
      }
    }
    return null;
  }

  private async runjsWrap(vm: SqlViewModel): Promise<SqlComResult> {
    const actQuery = this.calcFinalQuery(vm);
    const params = parseWhereParams(vm.WhereParams);
    const data = await this.context.queryMany(actQuery, params);
    const countRow = data.length > 1 && data[1].length > 0 ? data[1][0] : null;
    const countValue = countRow ? Number(getRowValue(countRow, "total")) : null;
    return { count: countValue, value: data[0] || [] };
  }
}
