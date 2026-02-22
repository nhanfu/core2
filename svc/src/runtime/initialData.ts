import type { Component, Feature, QueryResult, RuntimeContext, ScriptRunner, DataAdapter } from "./types.js";
import { formatTemplate, flattenComponents, parseJsonSafe } from "./utils.js";

type ComponentData = {
  componentId: string;
  fieldName?: string;
  data: Array<Record<string, unknown>>;
  total?: Array<Record<string, unknown>>;
};

export const loadInitialData = async (
  feature: Feature,
  context: RuntimeContext,
  adapter: DataAdapter,
  scriptRunner: ScriptRunner,
): Promise<ComponentData[]> => {
  const components = flattenComponents(feature).filter((component) => component.Query);
  if (components.length === 0) return [];

  const tasks = components.map(async (component) => {
    const query = parseJsonSafe<{ sql?: string; total?: string }>(component.Query || "");
    if (!query?.sql) return null;
    const params = component.PreQuery
      ? scriptRunner.evaluate<Record<string, unknown>>(component.PreQuery, { feature, component, context })
      : null;
    const sql = formatTemplate(query.sql, params || {});
    const totalSql = query.total ? formatTemplate(query.total, params || {}) : null;
    const data = await adapter.query(sql, { conn: context.dataConn });
    const total = totalSql ? await adapter.query(totalSql, { conn: context.dataConn }) : undefined;
    return {
      componentId: component.Id || component.FieldName || "",
      fieldName: component.FieldName,
      data,
      total,
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
