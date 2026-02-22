import type { Component, DataAdapter, Feature, MetadataStore, PatchVM, QueryRequest, QueryResult, RuntimeContext, ScriptRunner } from "./types.js";
import type { EventRegistry } from "./eventRegistry.js";
export declare class RuntimeEngine {
    private store;
    private adapter;
    private scriptRunner;
    private eventRegistry;
    constructor(store: MetadataStore, adapter: DataAdapter, scriptRunner: ScriptRunner, eventRegistry: EventRegistry);
    getFeature(name: string, context: RuntimeContext): Promise<Feature | null>;
    loadFeatureInitialData(name: string, context: RuntimeContext): Promise<QueryResult>;
    executeQuery(request: QueryRequest, context: RuntimeContext): Promise<QueryResult>;
    executePatch(patch: PatchVM, feature: Feature | undefined, component: Component | undefined, context: RuntimeContext): Promise<number>;
    executeEvent(feature: Feature | undefined, eventsJson: string | undefined, eventType: string, args?: unknown[]): Promise<unknown>;
    private interpolate;
}
//# sourceMappingURL=runtime.d.ts.map