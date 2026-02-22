export class EventRegistry {
  private handlers: Map<string, (...args: unknown[]) => unknown> = new Map();

  register(name: string, handler: (...args: unknown[]) => unknown): void {
    this.handlers.set(name, handler);
  }

  get(name: string): ((...args: unknown[]) => unknown) | undefined {
    return this.handlers.get(name);
  }
}
