export declare class DefaultScriptRunner {
    evaluate<T = unknown>(expression: string, scope?: Record<string, unknown>): T | null;
    invoke<T = unknown>(expression: string, scope?: Record<string, unknown>, args?: unknown[]): T | null;
    private run;
}
//# sourceMappingURL=scriptRunner.d.ts.map