import type { Component, Feature } from "./types.js";
export declare const SystemFields: string[];
export declare const isNullOrWhiteSpace: (value?: string | null) => boolean;
export declare const escapeSqlValue: (value: string | null | undefined) => string;
export declare const toLowerSafe: (value?: string | null) => string;
export declare const parseJsonSafe: <T>(value?: string | null) => T | null;
export declare const formatTemplate: (template: string, data: Record<string, unknown>) => string;
export declare const flattenComponents: (feature?: Feature | null) => Component[];
//# sourceMappingURL=utils.d.ts.map