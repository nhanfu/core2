/**
 * Query Service - Core dynamic SQL query execution service
 * Replaces the Jint-based QueryService from CoreAPI
 *
 * Provides methods for:
 * - Go(query): Execute SELECT with paging support
 * - Com(comQuery): Execute custom queries
 * - Sql(sql): Execute raw SQL
 * - CalcFinalQuery(sqlViewModel): Token replacement and query building
 * - ExecuteJsScript(script, context): Dynamic SQL generation via JavaScript
 */

import { query as dbQuery, execute as dbExecute } from "../database/postgresClient.ts";
import type { SqlViewModel, SqlQuery, UserContext, SqlComResult, QueryResult } from "../types/interfaces.ts";

// ============================================
// Token Replacement Types
// ============================================

/** Token context for query replacement */
export interface TokenContext {
  TokenUserId: string;
  TokenUserName: string;
  TokenFullName: string;
  TokenEmail: string;
  TokenRoleIds: string;
  TokenRoleNames: string;
  TokenDepartmentId: string;
  TokenPositionId: string;
  TokenTeamId: string;
  TokenPartnerId: string;
  TokenCenterIds: string;
  TokenTenantCode: string;
  TokenEnv: string;
  TokenConnKey: string;
  [key: string]: string; // Allow custom tokens
}

// ============================================
// Query Options
// ============================================

/** Options for Go query execution */
export interface GoOptions {
  /** Connection key */
  connKey?: string;
  /** Page number (1-based) */
  page?: number;
  /** Page size */
  pageSize?: number;
  /** Total record count flag */
  count?: boolean;
  /** Skip count */
  skip?: number;
  /** Top records */
  top?: number;
}

/** Custom query options */
export interface ComOptions {
  /** Connection key */
  connKey?: string;
  /** Query parameters */
  params?: Record<string, any>;
}

/** Raw SQL options */
export interface SqlOptions {
  /** Connection key */
  connKey?: string;
  /** Query parameters */
  params?: any[];
}

/** JavaScript execution options */
export interface JsExecutionOptions {
  /** Timeout in milliseconds (default: 5000) */
  timeout?: number;
  /** Additional context to pass to the script */
  context?: Record<string, any>;
}

// ============================================
// Token Context Factory
// ============================================

/**
 * Create a TokenContext from UserContext
 * @param userContext - The user context from authentication
 * @returns TokenContext with all token replacements
 */
export function createTokenContext(userContext?: UserContext): TokenContext {
  const defaultContext: TokenContext = {
    TokenUserId: "",
    TokenUserName: "",
    TokenFullName: "",
    TokenEmail: "",
    TokenRoleIds: "",
    TokenRoleNames: "",
    TokenDepartmentId: "",
    TokenPositionId: "",
    TokenTeamId: "",
    TokenPartnerId: "",
    TokenCenterIds: "",
    TokenTenantCode: "",
    TokenEnv: "",
    TokenConnKey: "",
  };

  if (!userContext) {
    return defaultContext;
  }

  return {
    TokenUserId: userContext.userId || "",
    TokenUserName: userContext.userName || "",
    TokenFullName: userContext.fullName || "",
    TokenEmail: userContext.email || "",
    TokenRoleIds: userContext.roles?.join(",") || "",
    TokenRoleNames: userContext.roleNames?.join(",") || "",
    TokenDepartmentId: userContext.departmentId || "",
    TokenPositionId: userContext.positionId || "",
    TokenTeamId: userContext.teamId || "",
    TokenPartnerId: userContext.partnerId || "",
    TokenCenterIds: userContext.centerIds?.join(",") || "",
    TokenTenantCode: userContext.tenantCode || "",
    TokenEnv: userContext.env || "",
    TokenConnKey: userContext.connKey || "",
  };
}

// ============================================
// Token Replacement
// ============================================

/**
 * Replace tokens in a query string
 * @param sql - The SQL query with tokens
 * @param context - The token context
 * @returns Query with tokens replaced
 */
export function replaceTokens(sql: string, context: TokenContext): string {
  let result = sql;

  // Replace all token placeholders
  for (const [key, value] of Object.entries(context)) {
    const token = `{${key}}`;
    // Handle empty values - replace with NULL or empty string based on context
    const replacement = value !== undefined && value !== null ? value : "";
    result = result.split(token).join(replacement);
  }

  return result;
}

// ============================================
// CalcFinalQuery - Build Final SQL Query
// ============================================

/**
 * Calculate final query from SqlViewModel with token replacement
 * @param sqlViewModel - The SQL view model
 * @param userContext - Optional user context for token replacement
 * @returns SqlQuery with built SQL statements
 */
export async function CalcFinalQuery(
  sqlViewModel: SqlViewModel,
  userContext?: UserContext
): Promise<SqlQuery> {
  const tokenContext = createTokenContext(userContext);

  // Build the SELECT clause
  let selectClause = sqlViewModel.select || "*";

  // Handle wrapping query for additional processing
  let mainQuery = "";

  if (sqlViewModel.wrapQuery) {
    // Wrap query in a subquery if needed
    mainQuery = `SELECT * FROM (${selectClause}) AS subq`;
  } else {
    mainQuery = `SELECT ${selectClause}`;
  }

  // Add FROM clause (table name)
  if (sqlViewModel.table) {
    mainQuery += ` FROM ${sqlViewModel.table}`;
  }

  // Add WHERE clause
  if (sqlViewModel.where) {
    let whereClause = replaceTokens(sqlViewModel.where, tokenContext);
    mainQuery += ` WHERE ${whereClause}`;
  }

  // Add GROUP BY clause
  if (sqlViewModel.groupBy) {
    mainQuery += ` GROUP BY ${sqlViewModel.groupBy}`;
  }

  // Add HAVING clause
  if (sqlViewModel.having) {
    let havingClause = replaceTokens(sqlViewModel.having, tokenContext);
    mainQuery += ` HAVING ${havingClause}`;
  }

  // Add ORDER BY clause
  if (sqlViewModel.orderBy) {
    mainQuery += ` ORDER BY ${sqlViewModel.orderBy}`;
  }

  // Add paging (TOP and OFFSET for SQL Server style)
  if (sqlViewModel.top && sqlViewModel.top > 0) {
    // Replace SELECT with SELECT TOP(n)
    mainQuery = mainQuery.replace(/^SELECT/i, `SELECT TOP(${sqlViewModel.top})`);
  }

  // Add OFFSET for paging
  if (sqlViewModel.skip && sqlViewModel.skip > 0) {
    mainQuery += ` OFFSET ${sqlViewModel.skip}`;
  }

  // Build the total count query
  let totalQuery = "";
  if (sqlViewModel.count) {
    // Create a count query from the main query
    let countBase = sqlViewModel.table
      ? `SELECT COUNT(*) as total FROM ${sqlViewModel.table}`
      : `SELECT COUNT(*) as total`;

    if (sqlViewModel.where) {
      let whereClause = replaceTokens(sqlViewModel.where, tokenContext);
      countBase += ` WHERE ${whereClause}`;
    }

    if (sqlViewModel.groupBy) {
      // If there's a GROUP BY, wrap in subquery to count
      totalQuery = `SELECT COUNT(*) as total FROM (${mainQuery}) AS count_subq`;
    } else {
      totalQuery = countBase;
    }
  }

  // Build the final SqlQuery result
  const result: SqlQuery = {
    sql: mainQuery,
  };

  if (totalQuery) {
    result.total = totalQuery;
  }

  return result;
}

// ============================================
// ExecuteJsScript - JavaScript Execution
// ============================================

/**
 * Execute JavaScript to generate SQL dynamically
 * Replaces Jint from .NET with native JavaScript Function constructor
 *
 * @param script - The JavaScript code to execute (arrow function or function body)
 * @param context - The context with tokens and additional data
 * @param options - Execution options (timeout, additional context)
 * @returns The generated SQL string
 */
export async function ExecuteJsScript(
  script: string,
  context: TokenContext,
  options?: JsExecutionOptions
): Promise<string> {
  const timeout = options?.timeout || 5000;
  const additionalContext = options?.context || {};

  // Merge contexts
  const fullContext = {
    ...context,
    ...additionalContext,
  };

  return new Promise((resolve, reject) => {
    // Set up timeout
    const timeoutId = setTimeout(() => {
      reject(new Error(`JavaScript execution timed out after ${timeout}ms`));
    }, timeout);

    try {
      let result: string;

      // Check if the script is an arrow function or needs wrapping
      if (script.trim().startsWith("(") || script.trim().startsWith("async")) {
        // Script is already in function form
        const fn = new Function(`"use strict"; return (${script})`)();

        // Execute the function with context
        if (typeof fn === "function") {
          result = fn(fullContext);
        } else {
          result = fn;
        }
      } else if (script.includes("=>")) {
        // Arrow function style: (context) => sql or context => sql
        const fn = new Function(`"use strict"; return (${script})`)();
        result = fn(fullContext);
      } else {
        // Plain code - wrap in a function that receives context
        const wrappedScript = `
          (function(context) {
            ${script}
          })
        `;
        const fn = new Function(wrappedScript)();
        result = fn(fullContext);
      }

      // Handle Promise (async functions)
      if (result && typeof result === "object" && typeof (result as any).then === "function") {
        (result as any).then((asyncResult: string) => {
          clearTimeout(timeoutId);
          resolve(asyncResult);
        }).catch((error: Error) => {
          clearTimeout(timeoutId);
          reject(error);
        });
      } else {
        clearTimeout(timeoutId);
        resolve(result);
      }
    } catch (error) {
      clearTimeout(timeoutId);
      reject(new Error(`JavaScript execution error: ${error instanceof Error ? error.message : String(error)}`));
    }
  });
}

// ============================================
// Go - Execute SELECT with Paging
// ============================================

/**
 * Execute a SELECT query with paging support
 * @param query - The query object or string
 * @param userContext - User context for token replacement
 * @param options - Query options (paging, count)
 * @returns Query result with data and optional total count
 */
export async function Go(
  query: string | SqlViewModel,
  userContext?: UserContext,
  options?: GoOptions
): Promise<QueryResult> {
  const page = options?.page || 1;
  const pageSize = options?.pageSize || 20;
  const skip = options?.skip || (page - 1) * pageSize;
  const top = options?.top || pageSize;
  const getCount = options?.count || false;

  let sql: string;
  let totalQuery: string | undefined;

  // If query is a string, use it directly with token replacement
  if (typeof query === "string") {
    const tokenContext = createTokenContext(userContext);
    sql = replaceTokens(query, tokenContext);
  } else {
    // Build query from SqlViewModel
    const sqlQuery = await CalcFinalQuery(query, userContext);
    sql = sqlQuery.sql;
    totalQuery = sqlQuery.total;
  }

  // Add paging to the query
  let pagedSql = sql;

  // Check if already has TOP clause
  if (!sql.toLowerCase().includes("top(") && !sql.toLowerCase().includes("limit")) {
    // Use TOP for SQL Server style (assuming PostgreSQL with supabase)
    pagedSql = sql.replace(/^SELECT/i, `SELECT TOP(${top})`);
  }

  // Add OFFSET for paging (PostgreSQL style)
  if (skip > 0) {
    if (pagedSql.toLowerCase().includes("offset")) {
      // Replace existing offset
      pagedSql = pagedSql.replace(/offset\s+\d+/i, `OFFSET ${skip}`);
    } else {
      pagedSql += ` OFFSET ${skip}`;
    }
  }

  // Execute the main query
  const data = await dbQuery(pagedSql);

  // Execute count query if requested
  let total = 0;
  if (getCount && totalQuery) {
    const countResult = await dbQuery(totalQuery);
    if (countResult && countResult.length > 0) {
      total = parseInt(countResult[0]?.total || "0", 10);
    }
  }

  return {
    result: {
      data,
      total,
      page,
      pageSize,
    },
    query: pagedSql,
    dataConn: options?.connKey || "default",
    metaConn: options?.connKey || "default",
  };
}

// ============================================
// Com - Execute Custom Query
// ============================================

/**
 * Execute a custom query
 * @param comQuery - The custom query configuration
 * @param userContext - User context for token replacement
 * @param options - Query options
 * @returns Custom query result
 */
export async function Com(
  comQuery: string | { sql: string; params?: any[] },
  userContext?: UserContext,
  options?: ComOptions
): Promise<SqlComResult> {
  let sql: string;

  if (typeof comQuery === "string") {
    // Simple string query with token replacement
    const tokenContext = createTokenContext(userContext);
    sql = replaceTokens(comQuery, tokenContext);
  } else {
    // Query object with SQL and params
    const tokenContext = createTokenContext(userContext);
    sql = replaceTokens(comQuery.sql, tokenContext);
  }

  // Execute the query
  const data = await dbQuery(sql, comQuery && typeof comQuery === "object" ? comQuery.params : undefined);

  return {
    value: data,
    count: data?.length || 0,
  };
}

// ============================================
// Sql - Execute Raw SQL
// ============================================

/**
 * Execute raw SQL directly
 * @param sql - The raw SQL to execute
 * @param params - Optional query parameters
 * @returns Query results
 */
export async function Sql(
  sql: string,
  params?: any[]
): Promise<any[]> {
  // Execute the raw SQL
  const data = await dbQuery(sql, params);
  return data;
}

// ============================================
// Export Default Service
// ============================================

/**
 * Query Service - Main export
 * Provides all query execution methods
 */
export const queryService = {
  Go,
  Com,
  Sql,
  CalcFinalQuery,
  ExecuteJsScript,
  createTokenContext,
  replaceTokens,
};

// Default export
export default queryService;
