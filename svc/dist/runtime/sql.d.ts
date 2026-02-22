import type { PatchVM } from "./types.js";
export declare class SqlBuilder {
    private userId;
    constructor(userId?: string);
    buildCreateOrUpdate(vm: PatchVM): string;
    buildUpdate(vm: PatchVM): string;
    private normalizePatch;
    private getIdField;
    private buildUpdateInternal;
    private buildInsertInternal;
}
//# sourceMappingURL=sql.d.ts.map