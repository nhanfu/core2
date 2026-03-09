import { Context, Next } from "https://deno.land/x/oak@v17.1.3/mod.ts";

/**
 * CORS middleware for Oak framework
 *
 * Handles Cross-Origin Resource Sharing (CORS) for the API:
 * - Allows all origins for development
 * - Sets appropriate CORS headers
 * - Handles preflight OPTIONS requests
 */
export async function cors(ctx: Context, next: Next): Promise<void> {
  // Set CORS headers for all responses
  ctx.response.headers.set("Access-Control-Allow-Origin", "*");
  ctx.response.headers.set(
    "Access-Control-Allow-Methods",
    "GET, POST, PUT, DELETE, PATCH, OPTIONS",
  );
  ctx.response.headers.set(
    "Access-Control-Allow-Headers",
    "Content-Type, Authorization, X-Requested-With, Accept, Origin",
  );
  ctx.response.headers.set("Access-Control-Max-Age", "86400");

  // Handle preflight OPTIONS requests
  if (ctx.request.method === "OPTIONS") {
    ctx.response.status = 204;
    return;
  }

  // Continue to the next middleware
  await next();
}
