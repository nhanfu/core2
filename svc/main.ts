/**
 * Main Entry Point
 * Oak web server for CoreAPI
 */

// Import Oak framework
import { Application } from "https://deno.land/x/oak@v17.1.3/mod.ts";

// Import middleware
import { cors } from "./src/middleware/cors.ts";
import { authentication } from "./src/middleware/authentication.ts";

// Import router
import { createRouter } from "./src/router.ts";

// Get port from environment or use default
const PORT = parseInt(Deno.env.get("PORT") || "8000", 10);

// Create Oak application
const app = new Application();

// Add middleware in order: CORS, Authentication, Router

// 1. CORS middleware first
app.use(cors);

// 2. Authentication middleware
app.use(authentication());

// 3. Router
const router = createRouter();
app.use(router.routes());
app.use(router.allowedMethods());

// 4. Error handling middleware (must be after router)
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

// Log startup message
console.log(`🚀 CoreAPI server starting on port ${PORT}`);
console.log(`📋 Health check available at http://localhost:${PORT}/health`);

// Start the server
await app.listen({ port: PORT });

console.log(`✅ Server stopped`);
