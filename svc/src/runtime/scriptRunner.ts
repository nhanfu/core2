import { isNullOrWhiteSpace } from "./utils.js";

export class DefaultScriptRunner {
  evaluate<T = unknown>(expression: string, scope?: Record<string, unknown>): T | null {
    if (isNullOrWhiteSpace(expression)) return null;
    const hasReturn = expression.includes("return");
    const code = hasReturn ? expression : `return ${expression}`;
    return this.run<T>(code, scope);
  }

  invoke<T = unknown>(expression: string, scope?: Record<string, unknown>, args?: unknown[]): T | null {
    if (isNullOrWhiteSpace(expression)) return null;
    return this.run<T>(expression, scope, args);
  }

  private run<T>(code: string, scope?: Record<string, unknown>, args: unknown[] = []): T | null {
    try {
      const keys = Object.keys(scope || {});
      const values = Object.values(scope || {});
      const fn = new Function(...keys, code) as (...params: unknown[]) => T;
      return fn(...values, ...args);
    } catch {
      return null;
    }
  }
}
