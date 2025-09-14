import { Hono } from "https://deno.land/x/hono@v4.3.11/mod.ts";
import { serve } from "https://deno.land/std@0.224.0/http/server.ts";
import { cors } from "https://deno.land/x/hono@v4.3.11/middleware.ts";
import { greetHandler, saveJsonHandler, getJsonHandler } from "./src/api/json_handler.ts";

// Initialize a new Hono app
const app = new Hono();

// Enable CORS for all routes
app.use("*", cors());

/* --- API ROUTE DEFINITIONS --- */

// A simple root endpoint to confirm the server is running
app.get("/", (c) => {
  return c.text("Welcome to the Hono API Server!");
});

// Greet endpoint that uses a URL parameter (e.g., /api/greet/Nhan)
app.get("/api/greet/:name", greetHandler);

// The endpoint to save JSON data to Supabase Storage
app.post("/api/saveJson", saveJsonHandler);
app.post("/api/feature/getFeature", getJsonHandler);

// A catch-all for 404 Not Found routes
app.notFound((c) => {
  return c.json({ error: 'Not Found' }, 404);
});

// Use Deno.serve to start the server.
// Hono's `fetch` method is the entry point for all requests.
serve(app.fetch);
