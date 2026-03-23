/**
 * Feature Controller
 * Handles all feature-related API endpoints
 * Migration from CoreAPI FeatureController
 */

import { verifyToken, type TokenPayload } from "../utils/jwt.ts";
import { queryService } from "../services/queryService.ts";
import { patchService } from "../services/patchService.ts";
import { metadataService } from "../services/metadataService.ts";
import type {
  UserContext,
  SqlViewModel,
  PatchVM,
  SqlResult,
  SqlComResult,
  QueryResult,
} from "../types/interfaces.ts";

// ============================================
// JWT Secret Configuration
// ============================================

// In production, this should come from environment variables
const JWT_SECRET = Deno.env.get("JWT_SECRET") || "your-secret-key-change-in-production";

// ============================================
// Request Body Types
// ============================================

/** Request body for /go endpoint */
export interface GoRequest {
  query: string | SqlViewModel;
  page?: number;
  pageSize?: number;
  count?: boolean;
}

/** Request body for /com endpoint */
export interface ComRequest {
  sql: string;
  params?: any[];
}

/** Request body for /sql endpoint */
export interface SqlRequest {
  sql: string;
  params?: any[];
}

/** Request body for /run endpoint */
export interface RunRequest {
  patchVM: PatchVM;
}

/** Request body for /delete endpoint */
export interface DeleteRequest {
  table: string;
  ids: string[];
  hardDelete?: boolean;
}

/** Request body for /loadFeature endpoint */
export interface LoadFeatureRequest {
  featureId: string;
}

// ============================================
// Middleware: Extract User Context from JWT
// ============================================

/**
 * Extract and verify JWT token from request headers
 * @param headers - Request headers
 * @returns UserContext extracted from token
 */
async function extractUserContext(headers: Headers): Promise<UserContext> {
  const authHeader = headers.get("Authorization");

  if (!authHeader) {
    throw new Error("Authorization header is required");
  }

  // Extract token from "Bearer <token>" format
  const token = authHeader.startsWith("Bearer ")
    ? authHeader.substring(7)
    : authHeader;

  if (!token) {
    throw new Error("Token is required");
  }

  // Verify and decode the token
  const payload = await verifyToken(token, JWT_SECRET);

  // Extract role IDs (can be string or string[])
  const roleIds = Array.isArray(payload.roleIds)
    ? payload.roleIds
    : payload.roleIds
      ? [payload.roleIds]
      : [];

  // Extract role names (can be string or string[])
  const roleNames = Array.isArray(payload.roleNameClaim)
    ? payload.roleNameClaim
    : payload.roleNameClaim
      ? [payload.roleNameClaim]
      : [];

  // Build user context
  const userContext: UserContext = {
    userId: payload.userId || "",
    tenantCode: payload.tenantCode || payload.tenantClaim || "system",
    env: "prod",
    connKey: "default",
    roles: roleIds,
    roleNames: roleNames,
    userName: payload.userName || "",
    email: payload.email || "",
    fullName: payload.fullName || "",
    departmentId: payload.departmentId,
    teamId: payload.teamId,
    partnerId: payload.partnerId,
    isAdmin: roleIds.includes("ADMIN") || roleIds.includes("admin"),
  };

  return userContext;
}

/**
 * Create a simplified handler that extracts user context
 */
function withUserContext<T>(
  handler: (userContext: UserContext, body: T) => Promise<Response>
) {
  return async (req: Request): Promise<Response> => {
    try {
      // Extract user context from JWT
      const userContext = await extractUserContext(req.headers);

      // Parse request body
      let body: T;
      const contentType = req.headers.get("Content-Type") || "";

      if (contentType.includes("application/json")) {
        body = await req.json();
      } else if (req.method === "GET" || req.method === "HEAD") {
        body = {} as T;
      } else {
        return new Response(
          JSON.stringify({ error: "Unsupported Content-Type" }),
          { status: 415, headers: { "Content-Type": "application/json" } }
        );
      }

      // Call the handler with user context and body
      return await handler(userContext, body);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown error";
      const status = message.includes("Authorization") ||
                     message.includes("Token") ? 401 : 500;

      return new Response(
        JSON.stringify({ error: message }),
        { status, headers: { "Content-Type": "application/json" } }
      );
    }
  };
}

// ============================================
// API Endpoints
// ============================================

/**
 * POST /api/feature/go
 * Execute query and return results with paging
 */
async function handleGo(
  userContext: UserContext,
  body: GoRequest
): Promise<Response> {
  try {
    const { query, page, pageSize, count } = body;

    if (!query) {
      return new Response(
        JSON.stringify({ error: "Query is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    const result: QueryResult = await queryService.Go(query, userContext, {
      page: page || 1,
      pageSize: pageSize || 20,
      count: count || false,
    });

    return new Response(JSON.stringify(result), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Query execution failed";
    return new Response(
      JSON.stringify({ error: message }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * POST /api/feature/com
 * Execute custom query
 */
async function handleCom(
  userContext: UserContext,
  body: ComRequest
): Promise<Response> {
  try {
    const { sql, params } = body;

    if (!sql) {
      return new Response(
        JSON.stringify({ error: "SQL query is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    const result: SqlComResult = await queryService.Com(
      { sql, params },
      userContext
    );

    return new Response(JSON.stringify(result), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Custom query execution failed";
    return new Response(
      JSON.stringify({ error: message }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * POST /api/feature/sql
 * Execute raw SQL
 */
async function handleSql(
  userContext: UserContext,
  body: SqlRequest
): Promise<Response> {
  try {
    const { sql, params } = body;

    if (!sql) {
      return new Response(
        JSON.stringify({ error: "SQL is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    // For raw SQL, we perform basic security checks
    const upperSql = sql.toUpperCase().trim();
    const forbidden = ["DROP", "TRUNCATE", "ALTER", "CREATE"];
    const hasForbidden = forbidden.some((kw) => upperSql.includes(kw));

    if (hasForbidden) {
      return new Response(
        JSON.stringify({ error: "This SQL operation is not allowed" }),
        { status: 403, headers: { "Content-Type": "application/json" } }
      );
    }

    const result = await queryService.Sql(sql, params);

    return new Response(JSON.stringify({ data: result }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "SQL execution failed";
    return new Response(
      JSON.stringify({ error: message }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * PATCH /api/feature/run
 * Save/create entity
 */
async function handleRun(
  userContext: UserContext,
  body: RunRequest
): Promise<Response> {
  try {
    const { patchVM } = body;

    if (!patchVM || !patchVM.table) {
      return new Response(
        JSON.stringify({ error: "Table name is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    // Set user context for the patch service
    patchService.setUserContext(userContext);

    // Execute the patch operation
    const result: SqlResult = await patchService.SavePatch2(patchVM);

    return new Response(JSON.stringify(result), {
      status: result.status || 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Save operation failed";
    return new Response(
      JSON.stringify({ error: message, status: 500 }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * DELETE /api/feature/delete
 * Delete entity
 */
async function handleDelete(
  userContext: UserContext,
  body: DeleteRequest
): Promise<Response> {
  try {
    const { table, ids, hardDelete = false } = body;

    if (!table) {
      return new Response(
        JSON.stringify({ error: "Table name is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    if (!ids || ids.length === 0) {
      return new Response(
        JSON.stringify({ error: "At least one ID is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    // Set user context for the patch service
    patchService.setUserContext(userContext);

    let result: SqlResult;

    if (hardDelete) {
      // Hard delete - permanent removal
      const success = await patchService.HardDelete(table, ids);
      result = {
        message: success ? "Delete successful" : "Delete failed",
        status: success ? 200 : 500,
      };
    } else {
      // Soft delete - set Active = false
      const unauthorized = await patchService.DeactivateAsync(table, ids);
      if (unauthorized.length > 0) {
        result = {
          message: `Unauthorized to delete: ${unauthorized.join(", ")}`,
          status: 403,
        };
      } else {
        result = {
          message: "Delete successful",
          status: 200,
        };
      }
    }

    return new Response(JSON.stringify(result), {
      status: result.status,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Delete operation failed";
    return new Response(
      JSON.stringify({ error: message, status: 500 }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * GET /api/feature/getMenu
 * Get menu items for the current user
 */
async function handleGetMenu(
  userContext: UserContext
): Promise<Response> {
  try {
    // Set user context for metadata service
    metadataService.setUserContext(userContext.tenantCode, userContext.roles);

    // Get menu items
    const menuItems = await metadataService.GetMenu(
      userContext.tenantCode,
      userContext.roles
    );

    return new Response(JSON.stringify({ data: menuItems }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Failed to get menu";
    return new Response(
      JSON.stringify({ error: message }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
}

/**
 * POST /api/feature/loadFeature
 * Load feature metadata
 */
async function handleLoadFeature(
  userContext: UserContext,
  body: LoadFeatureRequest
): Promise<Response> {
  try {
    const { featureId } = body;

    if (!featureId) {
      return new Response(
        JSON.stringify({ error: "Feature ID is required" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    // Set user context for metadata service
    metadataService.setUserContext(userContext.tenantCode, userContext.roles);

    // Load feature metadata
    const feature = await metadataService.LoadFeature(userContext.tenantCode, featureId);

    return new Response(JSON.stringify(feature), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  } catch (error) {
    const errorObj = error as Error & { statusCode?: number };
    const status = errorObj.statusCode || 500;
    const message = errorObj.message || "Failed to load feature";

    return new Response(
      JSON.stringify({ error: message }),
      { status, headers: { "Content-Type": "application/json" } }
    );
  }
}

// ============================================
// Main Request Handler
// ============================================

/**
 * Feature Controller - Main request handler
 * Routes requests to appropriate endpoint handlers
 */
export async function featureController(req: Request): Promise<Response> {
  const url = new URL(req.url);
  const pathname = url.pathname;
  const method = req.method;

  // Route: POST /api/feature/go
  if (pathname === "/api/feature/go" && method === "POST") {
    return withUserContext(handleGo)(req);
  }

  // Route: POST /api/feature/com
  if (pathname === "/api/feature/com" && method === "POST") {
    return withUserContext(handleCom)(req);
  }

  // Route: POST /api/feature/sql
  if (pathname === "/api/feature/sql" && method === "POST") {
    return withUserContext(handleSql)(req);
  }

  // Route: PATCH /api/feature/run
  if (pathname === "/api/feature/run" && method === "PATCH") {
    return withUserContext(handleRun)(req);
  }

  // Route: DELETE /api/feature/delete
  if (pathname === "/api/feature/delete" && method === "DELETE") {
    return withUserContext(handleDelete)(req);
  }

  // Route: GET /api/feature/getMenu
  if (pathname === "/api/feature/getMenu" && method === "GET") {
    return withUserContext(handleGetMenu)(req);
  }

  // Route: POST /api/feature/loadFeature
  if (pathname === "/api/feature/loadFeature" && method === "POST") {
    return withUserContext(handleLoadFeature)(req);
  }

  // 404 - Not Found
  return new Response(
    JSON.stringify({ error: "Endpoint not found" }),
    { status: 404, headers: { "Content-Type": "application/json" } }
  );
}

// Export for use in main router
export default featureController;

