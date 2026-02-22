export declare class EventRegistry {
    private handlers;
    register(name: string, handler: (...args: unknown[]) => unknown): void;
    get(name: string): ((...args: unknown[]) => unknown) | undefined;
}
//# sourceMappingURL=eventRegistry.d.ts.map