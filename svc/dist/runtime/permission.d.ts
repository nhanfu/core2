import type { Component, Feature, RuntimeContext } from "./types.js";
export type PermissionOperation = "read" | "write" | "delete" | "deactivate" | "export";
export declare const hasPermission: (feature: Feature | undefined, component: Component | undefined, context: RuntimeContext | undefined, operation: PermissionOperation, bypass?: boolean) => boolean;
//# sourceMappingURL=permission.d.ts.map