import type { DataAdapter } from "../types.js";

/**
 * Supabase PostgreSQL Adapter
 * Implements DataAdapter interface using Bun.SQL with PostgreSQL connection
 */
export class SupabaseAdapter implements DataAdapter {
  private connectionString: string;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private sql: any;

  constructor(connectionString: string) {
    this.connectionString = connectionString;
  }

  /**
   * Initialize the SQL connection
   */
  async initialize(): Promise<void> {
    if (typeof Bun !== "undefined" && (Bun as any).SQL) {
      this.sql = (Bun as any).SQL(this.connectionString);
    } else {
      throw new Error("Bun.SQL is not available. Please use Bun runtime.");
    }
  }

  /**
   * Execute a query and return results
   */
  async query(
    sql: string,
    options?: { params?: Record<string, unknown>; conn?: string }
  ): Promise<Array<Record<string, unknown>>> {
    if (!this.sql) {
      await this.initialize();
    }

    try {
      const params = options?.params || {};
      const paramValues = Object.values(params);
      
      if (this.sql) {
        const result = await this.sql.query(sql, ...paramValues);
        return this.mapToObjects(result);
      }
      
      return [];
    } catch (error) {
      console.error("Query error:", error);
      throw error;
    }
  }

  /**
   * Execute multiple queries and return array of results
   */
  async queryMany(
    sql: string,
    options?: { params?: Record<string, unknown>; conn?: string }
  ): Promise<Array<Array<Record<string, unknown>>>> {
    if (!this.sql) {
      await this.initialize();
    }

    try {
      const params = options?.params || {};
      const paramValues = Object.values(params);
      
      if (this.sql) {
        const result = await this.sql.query(sql, ...paramValues);
        return [this.mapToObjects(result)];
      }
      
      return [[]];
    } catch (error) {
      console.error("QueryMany error:", error);
      throw error;
    }
  }

  /**
   * Execute a statement (INSERT, UPDATE, DELETE)
   */
  async execute(
    sql: string,
    options?: { params?: Record<string, unknown>; conn?: string }
  ): Promise<number> {
    if (!this.sql) {
      await this.initialize();
    }

    try {
      const params = options?.params || {};
      const paramValues = Object.values(params);
      
      if (this.sql) {
        const result = await this.sql.query(sql, ...paramValues);
        return result.success ? 1 : 0;
      }
      
      return 0;
    } catch (error) {
      console.error("Execute error:", error);
      throw error;
    }
  }

  /**
   * Map Bun query result to array of objects
   */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private mapToObjects(result: any): Array<Record<string, unknown>> {
    if (!result) {
      return [];
    }

    // Handle different result formats from Bun.SQL
    if (result.columns && result.rows) {
      const { columns, rows } = result;
      return rows.map((row: unknown[]) => {
        const obj: Record<string, unknown> = {};
        columns.forEach((col: string, index: number) => {
          obj[col] = row[index];
        });
        return obj;
      });
    }

    // If result is already an array of objects
    if (Array.isArray(result)) {
      return result as Array<Record<string, unknown>>;
    }

    return [];
  }

  /**
   * Close the connection
   */
  async close(): Promise<void> {
    // Bun.SQL handles connection pooling automatically
    this.sql = undefined;
  }
}

/**
 * Create a Supabase adapter from connection string
 */
export function createSupabaseAdapter(connectionString: string): SupabaseAdapter {
  return new SupabaseAdapter(connectionString);
}

/**
 * Parse Supabase connection string
 * Format: postgresql://[user[:password]@][host[:port]][/database][?params]
 */
export function parseSupabaseConnectionString(connectionString: string): {
  user?: string;
  password?: string;
  host?: string;
  port?: string;
  database?: string;
  params?: Record<string, string>
} {
  const match = connectionString.match(
    /^postgresql:\/\/(?:(?<user>[^:@]+)(?::(?<password>[^:@]+))?@)?(?<host>[^:@\/]+)(?::(?<port>\d+))?\/(?<database>[^\?]+)?(?:\?(?<params>.+))?$/
  );

  if (!match) {
    throw new Error("Invalid Supabase connection string format");
  }

  const { user, password, host, port, database, params } = match.groups || {};

  let parsedParams: Record<string, string> = {};
  if (params) {
    params.split("&").forEach((param) => {
      const [key, value] = param.split("=");
      if (key && value) {
        parsedParams[key] = value;
      }
    });
  }

  return {
    user,
    password,
    host,
    port,
    database,
    params: parsedParams,
  };
}
