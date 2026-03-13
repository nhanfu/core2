/**
 * Authentication Middleware for Oak Framework
 * JWT validation middleware for protected routes
 */

import { Context } from "https://deno.land/x/oak@v17.1.3/mod.ts";
import type { Middleware } from "https://deno.land/x/oak@v17.1.3/mod.ts";
import { verifyToken, TokenPayload } from "../utils/jwt.ts";
import type { UserContext } from "../types/interfaces.ts";

// JWT configuration from environment
const JWT_SECRET = Deno.env.get("JWT_SECRET") || "your-secret-key";
const JWT_ISSUER = Deno.env.get("JWT_ISSUER") || "CoreAPI";
const JWT_AUDIENCE = Deno.env.get("JWT_AUDIENCE") || "CoreAPI";

// Public endpoints that don't require authentication
const PUBLIC_ENDPOINTS = [
  "/api/auth/login",
  "/api/auth/signin",
  "/api/auth/refreshToken",
  "/api/auth/refresh",
  "/api/auth/register",
  "/api/auth/signup",
  "/health",
  "/healthcheck",
];

/**
 * Extended Context interface to include user state
 */
export interface AuthState {
  user?: UserContext;
}

/**
 * Checks if a path is a public endpoint (no authentication required)
 * @param path - The request path
 * @returns true if the path is public
 */
function isPublicEndpoint(path: string): boolean {
  const normalizedPath = path
    .replace(/\/{2,}/g, "/")
    .replace(/\/$/, "") || "/";

  // Check exact matches
  if (PUBLIC_ENDPOINTS.some((endpoint) => normalizedPath === endpoint)) {
    return true;
  }

  // Allow the API to be mounted behind a path prefix such as /core/api/auth/login.
  if (PUBLIC_ENDPOINTS.some((endpoint) => normalizedPath.endsWith(endpoint))) {
    return true;
  }

  // Check if path starts with any public prefix
  const publicPrefixes = ["/public/", "/static/"];
  if (publicPrefixes.some((prefix) => normalizedPath.startsWith(prefix))) {
    return true;
  }

  return false;
}

/**
 * Extracts the Bearer token from the Authorization header
 * @param authHeader - The Authorization header value
 * @returns The token string or null if not found
 */
function extractBearerToken(authHeader: string | null): string | null {
  if (!authHeader) {
    return null;
  }

  const parts = authHeader.split(" ");
  if (parts.length !== 2 || parts[0].toLowerCase() !== "bearer") {
    return null;
  }

  return parts[1];
}

/**
 * Creates a UserContext object from the JWT payload
 * @param payload - The decoded JWT token payload
 * @returns UserContext object
 */
function createUserContext(payload: TokenPayload): UserContext {
  // Extract role IDs - handle both string and array formats
  let roleIds: string[] = [];
  if (payload.roleIds) {
    roleIds = Array.isArray(payload.roleIds)
      ? payload.roleIds
      : [payload.roleIds];
  }

  // Extract role names - handle both string and array formats
  let roleNames: string[] = [];
  if (payload.roleNameClaim) {
    roleNames = Array.isArray(payload.roleNameClaim)
      ? payload.roleNameClaim
      : [payload.roleNameClaim];
  } else if (payload.roleName) {
    roleNames = Array.isArray(payload.roleName)
      ? payload.roleName
      : [payload.roleName];
  }

  // Get tenant code from either TenantCode or TenantClaim
  const tenantCode = payload.tenantCode || payload.tenantClaim || "";

  // Check if user is admin (based on role names)
  const isAdmin = roleNames.some(
    (name) => name.toLowerCase() === "admin" || name.toLowerCase() === "administrator"
  );

  return {
    userId: payload.userId || "",
    tenantCode: tenantCode,
    env: (payload.env as string) || "production",
    connKey: (payload.connKey as string) || "default",
    roles: roleIds,
    roleNames: roleNames,
    userName: payload.userName || "",
    email: payload.email || "",
    fullName: payload.fullName || "",
    departmentId: payload.departmentId as string | undefined,
    positionId: payload.positionId as string | undefined,
    teamId: payload.teamId as string | undefined,
    partnerId: payload.partnerId as string | undefined,
    centerIds: (payload.centerIds as string[]) || [],
    isAdmin: isAdmin,
  };
}

/**
 * Authentication middleware for Oak framework
 *
 * This middleware:
 * 1. Extracts JWT token from Authorization header (Bearer token)
 * 2. Skips validation for public endpoints
 * 3. Verifies token signature and expiration
 * 4. Attaches decoded user context to request.state.user
 * 5. Calls next() for valid tokens
 * 6. Returns 401 for invalid/expired tokens
 *
 * @returns Oak middleware function
 */
export function authentication(): Middleware {
  return async (ctx: Context, next: () => Promise<unknown>): Promise<void> => {
    if (ctx.request.method === "OPTIONS") {
      await next();
      return;
    }

    // Get the request path
    const path = ctx.request.url.pathname;

    // Skip authentication for public endpoints
    if (isPublicEndpoint(path)) {
      await next();
      return;
    }

    // Extract token from Authorization header
    const authHeader = ctx.request.headers.get("Authorization");
    const token = extractBearerToken(authHeader);

    if (!token) {
      ctx.response.status = 401;
      ctx.response.body = {
        success: false,
        message: "Authorization token not provided",
        error: "MISSING_TOKEN",
      };
      return;
    }

    try {
      // Verify the token
      const payload = await verifyToken(token, JWT_SECRET);

      // Verify issuer if configured
      if (JWT_ISSUER && payload.iss !== JWT_ISSUER) {
        ctx.response.status = 401;
        ctx.response.body = {
          success: false,
          message: "Invalid token issuer",
          error: "INVALID_ISSUER",
        };
        return;
      }

      // Verify audience if configured
      if (JWT_AUDIENCE) {
        const aud = payload.aud;
        if (!aud || (Array.isArray(aud) && !aud.includes(JWT_AUDIENCE))) {
          ctx.response.status = 401;
          ctx.response.body = {
            success: false,
            message: "Invalid token audience",
            error: "INVALID_AUDIENCE",
          };
          return;
        }
      }

      // Create user context from payload
      const userContext = createUserContext(payload);

      // Attach user context to request state
      ctx.state.user = userContext;

      // Log authentication success (optional, for debugging)
      console.log(
        `[AUTH] User authenticated: ${userContext.userId} (${userContext.userName}) - ${path}`
      );

      // Continue to the next middleware/route handler
      await next();
    } catch (error) {
      // Handle token verification errors
      const errorMessage = error instanceof Error ? error.message : "Unknown error";

      // Determine error type for better client feedback
      let clientMessage = "Invalid or expired token";
      let errorCode = "INVALID_TOKEN";

      if (errorMessage.includes("expired")) {
        clientMessage = "Token has expired";
        errorCode = "TOKEN_EXPIRED";
      } else if (errorMessage.includes("signature")) {
        clientMessage = "Invalid token signature";
        errorCode = "INVALID_SIGNATURE";
      } else if (errorMessage.includes("issuer")) {
        clientMessage = "Invalid token issuer";
        errorCode = "INVALID_ISSUER";
      } else if (errorMessage.includes("audience")) {
        clientMessage = "Invalid token audience";
        errorCode = "INVALID_AUDIENCE";
      }

      ctx.response.status = 401;
      ctx.response.body = {
        success: false,
        message: clientMessage,
        error: errorCode,
      };

      // Log authentication failure (optional, for debugging)
      console.log(`[AUTH] Authentication failed: ${errorMessage} - ${path}`);
    }
  };
}

/**
 * Optional authentication middleware that doesn't block requests
 * Use this when you want to attach user context if token is present,
 * but allow requests to continue even without authentication
 *
 * @returns Oak middleware function
 */
export function optionalAuthentication(): Middleware {
  return async (ctx: Context, next: () => Promise<unknown>): Promise<void> => {
    // Extract token from Authorization header
    const authHeader = ctx.request.headers.get("Authorization");
    const token = extractBearerToken(authHeader);

    if (token) {
      try {
        // Verify the token
        const payload = await verifyToken(token, JWT_SECRET);

        // Create and attach user context
        const userContext = createUserContext(payload);
        ctx.state.user = userContext;
      } catch (error) {
        // Silently fail - token is optional
        console.log(`[AUTH] Optional auth failed: ${error instanceof Error ? error.message : "Unknown"}`);
      }
    }

    // Always continue to the next middleware/route handler
    await next();
  };
}

/**
 * Helper function to get current user from context
 * @param ctx - Oak Context
 * @returns UserContext or undefined
 */
export function getCurrentUser(ctx: Context): UserContext | undefined {
  return ctx.state.user as UserContext | undefined;
}

/**
 * Helper function to check if current user has a specific role
 * @param ctx - Oak Context
 * @param roleName - Role name to check
 * @returns true if user has the role
 */
export function hasRole(ctx: Context, roleName: string): boolean {
  const user = getCurrentUser(ctx);
  if (!user) {
    return false;
  }

  return user.roleNames.some(
    (name) => name.toLowerCase() === roleName.toLowerCase()
  );
}

/**
 * Helper function to check if current user is an admin
 * @param ctx - Oak Context
 * @returns true if user is an admin
 */
export function isAdmin(ctx: Context): boolean {
  const user = getCurrentUser(ctx);
  return user?.isAdmin || false;
}
