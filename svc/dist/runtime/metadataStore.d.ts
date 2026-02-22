import type { Feature, MetadataStore, RuntimeContext } from "./types.js";
export declare class FileMetadataStore implements MetadataStore {
    private baseDir;
    private tenant;
    constructor(baseDir: string, tenant?: string);
    getFeature(name: string, context?: RuntimeContext): Promise<Feature | null>;
    getPublicFeature(name: string, context?: RuntimeContext): Promise<Feature | null>;
}
//# sourceMappingURL=metadataStore.d.ts.map