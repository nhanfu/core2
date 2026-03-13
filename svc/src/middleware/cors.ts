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
  const requestOrigin = ctx.request.headers.get("Origin");
  const requestMethod = ctx.request.headers.get("Access-Control-Request-Method");
  const requestHeaders = ctx.request.headers.get("Access-Control-Request-Headers");
  const requestPrivateNetwork = ctx.request.headers.get("Access-Control-Request-Private-Network");
  const allowMethods = requestMethod ||
    "GET, POST, PUT, DELETE, PATCH, OPTIONS, HEAD";
  const allowHeaders = requestHeaders ||
    "Content-Type, Authorization, X-Requested-With, Accept, Origin, Cache-Control, Pragma";

  // Set CORS headers for all responses
  if (requestOrigin) {
    ctx.response.headers.set("Access-Control-Allow-Origin", requestOrigin);
    ctx.response.headers.set("Access-Control-Allow-Credentials", "true");
  } else {
    ctx.response.headers.set("Access-Control-Allow-Origin", "*");
  }
  ctx.response.headers.set("Access-Control-Allow-Methods", allowMethods);
  ctx.response.headers.set("Access-Control-Allow-Headers", allowHeaders);
  ctx.response.headers.set("Access-Control-Expose-Headers", "*");
  ctx.response.headers.set("Access-Control-Max-Age", "86400");
  ctx.response.headers.set(
    "Vary",
    "Origin, Access-Control-Request-Method, Access-Control-Request-Headers",
  );

  if (requestPrivateNetwork === "true") {
    ctx.response.headers.set("Access-Control-Allow-Private-Network", "true");
  }

  // Handle preflight OPTIONS requests
  if (ctx.request.method === "OPTIONS") {
    ctx.response.status = 204;
    return;
  }

  // Continue to the next middleware
  await next();
}
