// @bun
// src/runtime/utils.ts
var SystemFields = [
  "id",
  "insertedby",
  "inserteddate",
  "updatedby",
  "updateddate",
  "tenantcode"
];
var isNullOrWhiteSpace = (value) => {
  if (value === null || value === undefined)
    return true;
  return value.toString().trim() === "";
};
var escapeSqlValue = (value) => {
  if (value === null || value === undefined)
    return "null";
  return `N'${value.replace(/'/g, "''")}'`;
};
var toLowerSafe = (value) => {
  return (value || "").toLowerCase();
};
var parseJsonSafe = (value) => {
  if (value === null || value === undefined)
    return null;
  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
};
var formatTemplate = (template, data) => {
  return template.replace(/\{([^}]+)\}/g, (_, key) => {
    const value = data[key];
    if (value === null || value === undefined)
      return "";
    return String(value);
  });
};
var flattenComponents = (feature) => {
  if (!feature)
    return [];
  const roots = [];
  if (Array.isArray(feature.Components))
    roots.push(...feature.Components);
  if (Array.isArray(feature.ComponentGroup))
    roots.push(...feature.ComponentGroup);
  const stack = [...roots];
  const result = [];
  while (stack.length > 0) {
    const current = stack.pop();
    if (!current)
      continue;
    result.push(current);
    if (Array.isArray(current.Components)) {
      stack.push(...current.Components);
    }
    if (Array.isArray(current.ComponentGroup)) {
      stack.push(...current.ComponentGroup);
    }
  }
  return result;
};
// src/runtime/scriptRunner.ts
class DefaultScriptRunner {
  evaluate(expression, scope) {
    if (isNullOrWhiteSpace(expression))
      return null;
    const hasReturn = expression.includes("return");
    const code = hasReturn ? expression : `return ${expression}`;
    return this.run(code, scope);
  }
  invoke(expression, scope, args) {
    if (isNullOrWhiteSpace(expression))
      return null;
    return this.run(expression, scope, args);
  }
  run(code, scope, args = []) {
    try {
      const keys = Object.keys(scope || {});
      const values = Object.values(scope || {});
      const fn = new Function(...keys, code);
      return fn(...values, ...args);
    } catch {
      return null;
    }
  }
}
// src/runtime/eventRegistry.ts
class EventRegistry {
  handlers = new Map;
  register(name, handler) {
    this.handlers.set(name, handler);
  }
  get(name) {
    return this.handlers.get(name);
  }
}
// src/runtime/permission.ts
var roleMatches = (roleId, roleIds) => {
  if (!roleId || !roleIds)
    return false;
  return roleIds.includes(roleId);
};
var componentAllows = (component, operation) => {
  if (!component)
    return true;
  switch (operation) {
    case "read":
      return component.CanRead !== false && component.CanReadAll !== false;
    case "write":
      return component.CanWrite !== false && component.CanWriteAll !== false;
    case "delete":
      return component.CanDelete !== false && component.CanDeleteAll !== false;
    case "deactivate":
      return component.CanDeactivate !== false && component.CanDeactivateAll !== false;
    case "export":
      return component.CanExport !== false;
    default:
      return true;
  }
};
var policyAllows = (policy, operation) => {
  switch (operation) {
    case "read":
      return !!(policy.CanRead || policy.CanReadAll);
    case "write":
      return !!(policy.CanWrite || policy.CanWriteAll);
    case "delete":
      return !!(policy.CanDelete || policy.CanDeleteAll);
    case "deactivate":
      return !!(policy.CanDeactivate || policy.CanDeactivateAll);
    case "export":
      return !!policy.CanExport;
    default:
      return false;
  }
};
var hasPermission = (feature, component, context, operation, bypass = false) => {
  if (bypass)
    return true;
  if (!componentAllows(component, operation))
    return false;
  if (feature?.IsPublic)
    return true;
  const policies = feature?.FeaturePolicies || [];
  if (!policies.length)
    return false;
  return policies.some((policy) => roleMatches(policy.RoleId, context?.roleIds) && policyAllows(policy, operation));
};
// src/runtime/sql.ts
class SqlBuilder {
  userId;
  constructor(userId = "1") {
    this.userId = userId;
  }
  buildCreateOrUpdate(vm) {
    const normalized = this.normalizePatch(vm);
    const idField = this.getIdField(normalized.Changes || []);
    if (!idField) {
      throw new Error("Id cannot be null");
    }
    const oldId = idField.OldVal || null;
    if (oldId) {
      return this.buildUpdateInternal(normalized, oldId);
    }
    return this.buildInsertInternal(normalized, idField.Value);
  }
  buildUpdate(vm) {
    const normalized = this.normalizePatch(vm);
    const idField = this.getIdField(normalized.Changes || []);
    if (!idField || isNullOrWhiteSpace(idField.Value || undefined)) {
      throw new Error("Id cannot be null");
    }
    return this.buildUpdateInternal(normalized, idField.Value);
  }
  normalizePatch(vm) {
    if (!vm || isNullOrWhiteSpace(vm.Table || undefined) || !vm.Changes || vm.Changes.length === 0) {
      throw new Error("Table name and change details can not be empty");
    }
    const table = (vm.Table || "").trim();
    const changes = (vm.Changes || []).filter((patch) => {
      if (isNullOrWhiteSpace(patch.Field)) {
        throw new Error("Field name of the patch can not be empty");
      }
      const field = toLowerSafe(patch.Field);
      if (field === "id")
        return true;
      return !SystemFields.includes(field);
    }).map((patch) => ({
      ...patch,
      Field: patch.Field.trim(),
      Value: patch.Value ?? null,
      OldVal: patch.OldVal ?? null
    }));
    return { ...vm, Table: table, Changes: changes };
  }
  getIdField(changes) {
    const idField = changes.find((x) => x.Field === "Id");
    return idField || null;
  }
  buildUpdateInternal(vm, id) {
    const updateFields = (vm.Changes || []).filter((x) => toLowerSafe(x.Field) !== "id").map((x) => x.Value === null ? `[${x.Field}] = null` : `[${x.Field}] = ${escapeSqlValue(x.Value)}`);
    if (updateFields.length === 0)
      return "";
    const now = new Date().toISOString();
    return `update [${vm.Table}] set ${updateFields.join(", ")}, UpdatedBy = '${this.userId}', UpdatedDate = '${now}' where Id = '${id}';`;
  }
  buildInsertInternal(vm, id) {
    if (isNullOrWhiteSpace(id || undefined)) {
      throw new Error("Id cannot be null");
    }
    const valueFields = (vm.Changes || []).filter((x) => toLowerSafe(x.Field) !== "active" && toLowerSafe(x.Field) !== "id");
    const fields = valueFields.map((x) => `[${x.Field}]`);
    const values = valueFields.map((x) => x.Value === null ? "null" : escapeSqlValue(x.Value));
    if (fields.length === 0 || values.length === 0)
      return "";
    const now = new Date().toISOString();
    return `insert into [${vm.Table}] ([Id], [Active], [InsertedBy], [InsertedDate], ${fields.join(", ")}) values ('${id}', 1, '${this.userId}', '${now}', ${values.join(", ")});`;
  }
}
// src/runtime/initialData.ts
var loadInitialData = async (feature, context, adapter, scriptRunner) => {
  const components = flattenComponents(feature).filter((component) => component.Query);
  if (components.length === 0)
    return [];
  const tasks = components.map(async (component) => {
    const query = parseJsonSafe(component.Query || "");
    if (!query?.sql)
      return null;
    const params = component.PreQuery ? scriptRunner.evaluate(component.PreQuery, { feature, component, context }) : null;
    const sql = formatTemplate(query.sql, params || {});
    const totalSql = query.total ? formatTemplate(query.total, params || {}) : null;
    const data = await adapter.query(sql, { conn: context.dataConn });
    const total = totalSql ? await adapter.query(totalSql, { conn: context.dataConn }) : undefined;
    return {
      componentId: component.Id || component.FieldName || "",
      fieldName: component.FieldName,
      data,
      total
    };
  });
  const results = await Promise.all(tasks);
  return results.filter((item) => !!item && item.componentId !== "");
};
var asQueryResult = (items) => {
  const data = items.map((item) => item.data);
  const total = items.map((item) => item.total || []);
  return { data, total };
};
// src/runtime/metadataStore.ts
import path from "path";
import { readFile } from "fs/promises";
var readJson = async (filePath) => {
  try {
    if (typeof Bun !== "undefined") {
      const text2 = await Bun.file(filePath).text();
      return JSON.parse(text2);
    }
    const text = await readFile(filePath, "utf-8");
    return JSON.parse(text);
  } catch {
    return null;
  }
};

class FileMetadataStore {
  baseDir;
  tenant;
  constructor(baseDir, tenant = "system") {
    this.baseDir = baseDir;
    this.tenant = tenant;
  }
  async getFeature(name, context) {
    if (isNullOrWhiteSpace(name))
      return null;
    const tenant = (context?.tenant || this.tenant).toLowerCase();
    const featurePath = path.join(this.baseDir, tenant, "features", `${name}.json`);
    return readJson(featurePath);
  }
  async getPublicFeature(name, context) {
    return this.getFeature(name, context);
  }
}
// src/runtime/runtime.ts
class RuntimeEngine {
  store;
  adapter;
  scriptRunner;
  eventRegistry;
  constructor(store, adapter, scriptRunner, eventRegistry) {
    this.store = store;
    this.adapter = adapter;
    this.scriptRunner = scriptRunner;
    this.eventRegistry = eventRegistry;
  }
  async getFeature(name, context) {
    return this.store.getFeature(name, context);
  }
  async loadFeatureInitialData(name, context) {
    const feature = await this.getFeature(name, context);
    if (!feature)
      return { data: [], total: [] };
    const items = await loadInitialData(feature, context, this.adapter, this.scriptRunner);
    return asQueryResult(items);
  }
  async executeQuery(request, context) {
    const feature = request.feature || (request.component ? undefined : null);
    const component = request.component || null;
    if (!component || !component.Query) {
      return { data: [], total: [] };
    }
    const parsed = parseJsonSafe(component.Query);
    if (!parsed?.sql)
      return { data: [], total: [] };
    const params = component.PreQuery ? this.scriptRunner.evaluate(component.PreQuery, { feature, component, context, params: request.params }) : request.params || {};
    const safeParams = params || {};
    const data = await this.adapter.query(this.interpolate(parsed.sql, safeParams), { conn: context.dataConn });
    const total = parsed.total ? await this.adapter.query(this.interpolate(parsed.total, safeParams), { conn: context.dataConn }) : [];
    return { data: [data], total: [total] };
  }
  async executePatch(patch, feature, component, context) {
    const allow = hasPermission(feature, component, context, "write", patch.ByPassPerm === true);
    if (!allow) {
      throw new Error("Unauthorized to write");
    }
    const builder = new SqlBuilder(context.userId || "1");
    const sql = patch.Update ? builder.buildUpdate(patch) : builder.buildCreateOrUpdate(patch);
    if (!sql)
      return 0;
    return this.adapter.execute(sql, { conn: context.dataConn });
  }
  async executeEvent(feature, eventsJson, eventType, args = []) {
    if (!eventsJson)
      return null;
    const events = parseJsonSafe(eventsJson);
    if (!events)
      return null;
    const handlerName = events[eventType];
    if (!handlerName)
      return null;
    const handler = this.eventRegistry.get(handlerName);
    if (handler) {
      return handler(...args);
    }
    return this.scriptRunner.invoke(handlerName, { feature }, args);
  }
  interpolate(template, params) {
    return template.replace(/\{([^}]+)\}/g, (_, key) => {
      const value = params[key];
      if (value === null || value === undefined)
        return "";
      return String(value);
    });
  }
}
export {
  toLowerSafe,
  parseJsonSafe,
  loadInitialData,
  isNullOrWhiteSpace,
  hasPermission,
  formatTemplate,
  flattenComponents,
  escapeSqlValue,
  asQueryResult,
  SystemFields,
  SqlBuilder,
  RuntimeEngine,
  FileMetadataStore,
  EventRegistry,
  DefaultScriptRunner
};
