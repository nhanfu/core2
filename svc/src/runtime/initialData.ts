import type { Component, Feature, QueryResult, RuntimeContext, ScriptRunner, DataAdapter } from "./types.js";
import { formatTemplate, flattenComponents, parseJsonSafe } from "./utils.js";

type ComponentData = {
  componentId: string;
  fieldName?: string;
  data: Array<Record<string, unknown>>;
  total?: Array<Record<string, unknown>>;
};

type SqlQuery = {
  sql?: string;
  total?: string;
};

const parseQueryValue = (value?: string | null): SqlQuery | Array<Record<string, unknown>> | null => {
  if (!value) return null;
  const parsed = parseJsonSafe<unknown>(value);
  if (!parsed) return null;
  if (Array.isArray(parsed)) return parsed as Array<Record<string, unknown>>;
  if (typeof parsed === "object") return parsed as SqlQuery;
  return null;
};

const resolveContextData = (component: Component, context: RuntimeContext): Array<Record<string, unknown>> | null => {
  const variables = context?.variables;
  if (!variables) return null;
  const keys = [component.EntityName, component.TableName, component.FieldName, component.Id].filter(
    (key): key is string => !!key && key.trim() !== "",
  );
  for (const key of keys) {
    if (!Object.prototype.hasOwnProperty.call(variables, key)) continue;
    const value = variables[key];
    if (Array.isArray(value)) return value as Array<Record<string, unknown>>;
    if (value && typeof value === "object") return [value as Record<string, unknown>];
  }
  return null;
};

export const loadInitialData = async (
  feature: Feature,
  context: RuntimeContext,
  adapter: DataAdapter,
  scriptRunner: ScriptRunner,
): Promise<ComponentData[]> => {
  const components = flattenComponents(feature).filter((component) => component.Query || !!resolveContextData(component, context));
  if (components.length === 0) return [];

  const tasks = components.map(async (component) => {
    const parsedQuery = parseQueryValue(component.Query || "");
    if (Array.isArray(parsedQuery)) {
      return {
        componentId: component.Id || component.FieldName || "",
        fieldName: component.FieldName,
        data: parsedQuery,
        total: [],
      } as ComponentData;
    }
    if (parsedQuery?.sql) {
      const params = component.PreQuery
        ? scriptRunner.evaluate<Record<string, unknown>>(component.PreQuery, { feature, component, context })
        : null;
      const sql = formatTemplate(parsedQuery.sql, params || {});
      const totalSql = parsedQuery.total ? formatTemplate(parsedQuery.total, params || {}) : null;
      const data = await adapter.query(sql, { conn: context.dataConn });
      const total = totalSql ? await adapter.query(totalSql, { conn: context.dataConn }) : undefined;
      return {
        componentId: component.Id || component.FieldName || "",
        fieldName: component.FieldName,
        data,
        total,
      } as ComponentData;
    }
    const contextData = resolveContextData(component, context);
    if (!contextData) return null;
    return {
      componentId: component.Id || component.FieldName || "",
      fieldName: component.FieldName,
      data: contextData,
      total: [],
    } as ComponentData;
  });

  const results = await Promise.all(tasks);
  return results.filter((item): item is ComponentData => !!item && item.componentId !== "");
};

export const asQueryResult = (items: ComponentData[]): QueryResult => {
  const data = items.map((item) => item.data);
  const total = items.map((item) => item.total || []);
  return { data, total };
};
