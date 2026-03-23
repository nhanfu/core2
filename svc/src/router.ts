/**
 * Main Router
 * Central routing configuration for all API endpoints
 * Uses Oak framework middleware and routing
 */

import { Application, Router, Context } from "https://deno.land/x/oak@v17.1.3/mod.ts";
import { authentication } from "./middleware/authentication.ts";
import { cors } from "./middleware/cors.ts";
import { login as authLogin, refreshToken as authrefreshToken, logout as authLogout } from "./controllers/authController.ts";
import { default as featureController } from "./controllers/featureController.ts";
import * as fileController from "./controllers/fileController.ts";
import type { UserContext } from "./types/interfaces.ts";

// ============================================
// Helper: Get User Context from Oak Context
// ============================================

/**
 * Extract user context from Oak context state
 * Assumes authentication middleware has populated ctx.state.user
 */
function getUserContext(ctx: Context): UserContext | undefined {
  return ctx.state.user as UserContext | undefined;
}

// ============================================
// Auth Controller Wrappers
// ============================================

/**
 * Wrapper for authController.login to work with Oak Context
 */
async function signIn(ctx: Context): Promise<void> {
  const bodyText = await ctx.request.body.text();
  const body = JSON.parse(bodyText);
  const result = await authLogin(body);

  ctx.response.status = result.statusCode;
  ctx.response.body = result;
}

/**
 * Wrapper for authController.refreshToken to work with Oak Context
 */
async function refreshToken(ctx: Context): Promise<void> {
  const bodyText = await ctx.request.body.text();
  const body = JSON.parse(bodyText);
  const result = await authrefreshToken(body);

  ctx.response.status = result.statusCode;
  ctx.response.body = result;
}

/**
 * Wrapper for authController.logout to work with Oak Context
 */
async function logout(ctx: Context): Promise<void> {
  const bodyText = await ctx.request.body.text();
  const body = JSON.parse(bodyText);
  const result = await authLogout(body);

  ctx.response.status = result.statusCode;
  ctx.response.body = result;
}

// ============================================
// Feature Controller Wrappers
// ============================================

/**
 * Wrapper for POST /api/feature/go
 * Convert Oak Context to native Request for featureController
 */
async function handleGo(ctx: Context): Promise<void> {
  // Create a native Request from the Oak context
  // The featureController will extract user context from the JWT in headers
  const request = new Request(ctx.request.url.href, {
    method: "POST",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for POST /api/feature/com
 */
async function handleCom(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "POST",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for POST /api/feature/sql
 */
async function handleSql(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "POST",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for PATCH /api/feature/run
 */
async function handleRun(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "PATCH",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for DELETE /api/feature/delete
 */
async function handleDelete(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "DELETE",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for GET /api/feature/getMenu
 */
async function handleGetMenu(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "GET",
    headers: ctx.request.headers,
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

/**
 * Wrapper for POST /api/feature/loadFeature
 */
async function handleLoadFeature(ctx: Context): Promise<void> {
  const request = new Request(ctx.request.url.href, {
    method: "POST",
    headers: ctx.request.headers,
    body: JSON.stringify(await JSON.parse(await ctx.request.body.text())),
  });

  const response = await featureController(request);
  ctx.response.status = response.status;
  ctx.response.body = await response.json();
}

// ============================================
// Create Router
// ============================================

/**
 * Create and configure the main application router
 */
export function createRouter(): Router {
  const router = new Router();

  // --------------------------------------------
  // Health Check (Public - no authentication required)
  // --------------------------------------------
  router.get("/health", (ctx) => {
    ctx.response.status = 200;
    ctx.response.body = {
      success: true,
      status: "healthy",
      timestamp: new Date().toISOString(),
    };
  });

  // --------------------------------------------
  // Auth Routes (Public - no authentication required)
  // --------------------------------------------
  router.post("/api/auth/login", signIn);
  router.post("/api/auth/refreshToken", refreshToken);

  // --------------------------------------------
  // Auth Routes (Protected - authentication required)
  // --------------------------------------------
  router.post("/api/auth/logout", logout);

  // --------------------------------------------
  // Feature Routes (Protected)
  // --------------------------------------------
  router.post("/api/feature/go", handleGo);
  router.post("/api/feature/com", handleCom);
  router.post("/api/feature/sql", handleSql);
  router.patch("/api/feature/run", handleRun);
  router.delete("/api/feature/delete", handleDelete);
  router.get("/api/feature/getMenu", handleGetMenu);
  router.post("/api/feature/loadFeature", handleLoadFeature);

  // --------------------------------------------
  // File Upload Routes (Protected)
  // --------------------------------------------
  router.post("/api/fileUpload/file", fileController.uploadFile);
  router.get("/api/fileUpload/:path", fileController.downloadFile);

  return router;
}

// ============================================
// Create Application
// ============================================

/**
 * Create the Oak application with middleware
 */
export function createApp(): Application {
  const app = new Application();

  // Error handling should wrap the full middleware chain.
  app.use(async (ctx, next) => {
    try {
      await next();
    } catch (err) {
      console.error("Unhandled error:", err);
      ctx.response.status = 500;
      ctx.response.body = {
        success: false,
        message: "Internal server error",
        error: err instanceof Error ? err.message : "Unknown error",
      };
    }
  });

  app.use(cors);
  app.use(authentication());

  // Add the router
  const router = createRouter();
  app.use(router.routes());
  app.use(router.allowedMethods());

  return app;
}

// Export default
export default createApp;

