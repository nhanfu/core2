import type {
  ActionRequest,
  Component,
  DataAdapter,
  Feature,
  MetadataStore,
  PatchVM,
  QueryRequest,
  QueryResult,
  RuntimeContext,
  ScriptRunner,
} from "./types.js";
import type { EventRegistry } from "./eventRegistry.js";
import { asQueryResult, loadInitialData } from "./initialData.js";
import { hasPermission } from "./permission.js";
import { parseJsonSafe } from "./utils.js";
import { SqlBuilder } from "./sql.js";

export class RuntimeEngine {
  constructor(
    private store: MetadataStore,
    private adapter: DataAdapter,
    private scriptRunner: ScriptRunner,
    private eventRegistry: EventRegistry,
  ) {}

  async getFeature(name: string, context: RuntimeContext): Promise<Feature | null> {
    return this.store.getFeature(name, context);
  }

  async loadFeatureInitialData(name: string, context: RuntimeContext): Promise<QueryResult> {
    const feature = await this.getFeature(name, context);
    if (!feature) return { data: [], total: [] };
    const items = await loadInitialData(feature, context, this.adapter, this.scriptRunner);
    return asQueryResult(items);
  }

  async executeQuery(request: QueryRequest, context: RuntimeContext): Promise<QueryResult> {
    const feature = request.feature || (request.component ? undefined : null);
    const component = request.component || null;
    if (!component || !component.Query) {
      return { data: [], total: [] };
    }
    const parsed = parseJsonSafe<{ sql?: string; total?: string }>(component.Query);
    if (!parsed?.sql) return { data: [], total: [] };
    const params = component.PreQuery
      ? this.scriptRunner.evaluate<Record<string, unknown>>(component.PreQuery, { feature, component, context, params: request.params })
      : request.params || {};
    const safeParams = params || {};
    const data = await this.adapter.query(this.interpolate(parsed.sql, safeParams), { conn: context.dataConn });
    const total = parsed.total ? await this.adapter.query(this.interpolate(parsed.total, safeParams), { conn: context.dataConn }) : [];
    return { data: [data], total: [total] };
  }

  async executePatch(patch: PatchVM, feature: Feature | undefined, component: Component | undefined, context: RuntimeContext): Promise<number> {
    const allow = hasPermission(feature, component, context, "write", patch.ByPassPerm === true);
    if (!allow) {
      throw new Error("Unauthorized to write");
    }
    const builder = new SqlBuilder(context.userId || "1", context.sqlDialect);
    const sql = patch.Update ? builder.buildUpdate(patch) : builder.buildCreateOrUpdate(patch);
    if (!sql) return 0;
    return this.adapter.execute(sql, { conn: context.dataConn });
  }

  async executeEvent(feature: Feature | undefined, eventsJson: string | undefined, eventType: string, args: unknown[] = []): Promise<unknown> {
    if (!eventsJson) return null;
    const events = parseJsonSafe<Record<string, string>>(eventsJson);
    if (!events) return null;
    const handlerName = events[eventType];
    if (!handlerName) return null;
    const handler = this.eventRegistry.get(handlerName);
    if (handler) {
      return handler(...args);
    }
    return this.scriptRunner.invoke(handlerName, { feature }, args);
  }

  private interpolate(template: string, params: Record<string, unknown>): string {
    return template.replace(/\{([^}]+)\}/g, (_, key: string) => {
      const value = params[key];
      if (value === null || value === undefined) return "";
      return String(value);
    });
  }
}
