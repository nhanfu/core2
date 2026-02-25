import type { ScriptRunner as IScriptRunner } from "./types.js";

/**
 * Script Execution Engine
 * Provides sandboxed script execution with safe API access
 */
export class ScriptRunner implements IScriptRunner {
  private globalScope: Record<string, unknown>;

  constructor(globalScope?: Record<string, unknown>) {
    this.globalScope = globalScope || {};
  }

  /**
   * Evaluate a simple expression and return the result
   */
  evaluate<T = unknown>(expression: string, scope?: Record<string, unknown>): T | null {
    try {
      // Create a safe scope by merging global and local scope
      const safeScope = { ...this.globalScope, ...scope };
      
      // Create function with limited scope
      const keys = Object.keys(safeScope);
      const values = Object.values(safeScope);
      
      // Use Function constructor with limited scope (still not fully secure for production)
      const fn = new Function(...keys, `return ${expression}`);
      const result = fn(...values);
      
      return result as T;
    } catch (error) {
      console.error("Script evaluation error:", error);
      return null;
    }
  }

  /**
   * Invoke a function with the given scope and arguments
   */
  invoke<T = unknown>(
    expression: string,
    scope?: Record<string, unknown>,
    args?: unknown[]
  ): T | null {
    try {
      // Create a safe scope by merging global scope, local scope, and arguments
      const safeScope = { 
        ...this.globalScope, 
        ...scope,
        args: args || []
      };
      
      // Create function with limited scope
      const keys = Object.keys(safeScope);
      const values = Object.values(safeScope);
      
      // Use Function constructor with limited scope
      const fn = new Function(...keys, expression);
      const result = fn(...values);
      
      return result as T;
    } catch (error) {
      console.error("Script invocation error:", error);
      return null;
    }
  }

  /**
   * Execute an async function with the given scope and arguments
   */
  async invokeAsync<T = unknown>(
    expression: string,
    scope?: Record<string, unknown>,
    args?: unknown[]
  ): Promise<T | null> {
    try {
      // Create a safe scope by merging global scope, local scope, and arguments
      const safeScope = { 
        ...this.globalScope, 
        ...scope,
        args: args || [],
        Promise: Promise
      };
      
      // Create async function with limited scope
      const keys = Object.keys(safeScope);
      const values = Object.values(safeScope);
      
      // Wrap in async function
      const fn = new Function(...keys, `return (async () => { ${expression} })()`);
      const result = await fn(...values);
      
      return result as T;
    } catch (error) {
      console.error("Async script invocation error:", error);
      return null;
    }
  }

  /**
   * Add a global function or value to the scope
   */
  setGlobal(key: string, value: unknown): void {
    this.globalScope[key] = value;
  }

  /**
   * Add multiple global functions or values to the scope
   */
  setGlobals(values: Record<string, unknown>): void {
    Object.assign(this.globalScope, values);
  }

  /**
   * Get a global value from the scope
   */
  getGlobal(key: string): unknown {
    return this.globalScope[key];
  }

  /**
   * Remove a global value from the scope
   */
  removeGlobal(key: string): void {
    delete this.globalScope[key];
  }

  /**
   * Clear all global values
   */
  clearGlobal(): void {
    this.globalScope = {};
  }

  /**
   * Create a child script runner with inherited global scope
   */
  createChild(additionalScope?: Record<string, unknown>): ScriptRunner {
    return new ScriptRunner({
      ...this.globalScope,
      ...additionalScope
    });
  }
}

/**
 * Create a ScriptRunner instance with default utilities
 */
export function createScriptRunner(): ScriptRunner {
  const runner = new ScriptRunner();
  
  // Add common utility functions
  runner.setGlobals({
    // Math utilities
    abs: Math.abs,
    ceil: Math.ceil,
    floor: Math.floor,
    round: Math.round,
    max: Math.max,
    min: Math.min,
    random: Math.random,
    sqrt: Math.sqrt,
    pow: Math.pow,
    
    // String utilities
    len: (s: string) => s?.length ?? 0,
    upper: (s: string) => s?.toUpperCase() ?? "",
    lower: (s: string) => s?.toLowerCase() ?? "",
    trim: (s: string) => s?.trim() ?? "",
    substr: (s: string, start: number, length?: number) => s?.substring(start, length ? start + length : undefined) ?? "",
    replace: (s: string, search: string, replace: string) => s?.replace(search, replace) ?? "",
    split: (s: string, separator: string) => s?.split(separator) ?? [],
    join: (arr: unknown[], separator: string) => Array.isArray(arr) ? arr.join(separator) : "",
    
    // Array utilities
    isArray: Array.isArray,
    arrLen: (arr: unknown[]) => Array.isArray(arr) ? arr.length : 0,
    arrFirst: (arr: unknown[]) => Array.isArray(arr) && arr.length > 0 ? arr[0] : null,
    arrLast: (arr: unknown[]) => Array.isArray(arr) && arr.length > 0 ? arr[arr.length - 1] : null,
    arrMap: (arr: unknown[], fn: (item: unknown, index: number) => unknown) => Array.isArray(arr) ? arr.map(fn) : [],
    arrFilter: (arr: unknown[], fn: (item: unknown, index: number) => boolean) => Array.isArray(arr) ? arr.filter(fn) : [],
    arrFind: (arr: unknown[], fn: (item: unknown, index: number) => boolean) => Array.isArray(arr) ? arr.find(fn) : null,
    
    // Object utilities
    isNull: (v: unknown) => v === null,
    isUndefined: (v: unknown) => v === undefined,
    isEmpty: (v: unknown) => v === null || v === undefined || v === "" || (Array.isArray(v) && v.length === 0),
    hasProp: (obj: unknown, prop: string) => obj && typeof obj === "object" && prop in obj,
    
    // Date utilities
    now: () => new Date(),
    date: (year: number, month: number, day: number) => new Date(year, month, day),
    timestamp: () => Date.now(),
    
    // Conversion utilities
    toNumber: (v: unknown) => Number(v),
    toString: (v: unknown) => String(v),
    toBool: (v: unknown) => Boolean(v),
    parseInt: (v: unknown, radix?: number) => parseInt(String(v), radix),
    parseFloat: (v: unknown) => parseFloat(String(v)),
    parseJson: (s: string) => { try { return JSON.parse(s); } catch { return null; } },
    stringify: (v: unknown) => JSON.stringify(v),
    
    // Formatting utilities
    formatDate: (date: Date, format: string) => {
      const d = new Date(date);
      return format
        .replace("YYYY", String(d.getFullYear()))
        .replace("MM", String(d.getMonth() + 1).padStart(2, "0"))
        .replace("DD", String(d.getDate()).padStart(2, "0"))
        .replace("HH", String(d.getHours()).padStart(2, "0"))
        .replace("mm", String(d.getMinutes()).padStart(2, "0"))
        .replace("ss", String(d.getSeconds()).padStart(2, "0"));
    },
    
    // Comparison utilities
    eq: (a: unknown, b: unknown) => a === b,
    ne: (a: unknown, b: unknown) => a !== b,
    gt: (a: number, b: number) => a > b,
    gte: (a: number, b: number) => a >= b,
    lt: (a: number, b: number) => a < b,
    lte: (a: number, b: number) => a <= b,
    
    // Null coalescing
    coalesce: (...args: unknown[]) => args.find(a => a !== null && a !== undefined),
    defaultTo: (v: unknown, defaultVal: unknown) => v === null || v === undefined ? defaultVal : v,
  });
  
  return runner;
}

/**
 * Execute a simple expression without creating a runner instance
 */
export function evaluate<T = unknown>(expression: string, scope?: Record<string, unknown>): T | null {
  const runner = createScriptRunner();
  return runner.evaluate<T>(expression, scope);
}

/**
 * Execute a script without creating a runner instance
 */
export function invoke<T = unknown>(expression: string, scope?: Record<string, unknown>, args?: unknown[]): T | null {
  const runner = createScriptRunner();
  return runner.invoke<T>(expression, scope, args);
}
