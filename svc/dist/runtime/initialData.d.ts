import type { Feature, QueryResult, RuntimeContext, ScriptRunner, DataAdapter } from "./types.js";
type ComponentData = {
    componentId: string;
    fieldName?: string;
    data: Array<Record<string, unknown>>;
    total?: Array<Record<string, unknown>>;
};
export declare const loadInitialData: (feature: Feature, context: RuntimeContext, adapter: DataAdapter, scriptRunner: ScriptRunner) => Promise<ComponentData[]>;
export declare const asQueryResult: (items: ComponentData[]) => QueryResult;
export {};
//# sourceMappingURL=initialData.d.ts.map