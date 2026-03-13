import { assertEquals } from "jsr:@std/assert@1";
import { Application } from "https://deno.land/x/oak@v17.1.3/mod.ts";
import { authentication } from "./authentication.ts";

function createTestApp(): Application {
  const app = new Application();
  app.use(authentication());
  app.use((ctx) => {
    ctx.response.status = 204;
  });
  return app;
}

async function dispatch(path: string, method = "GET"): Promise<Response> {
  const app = createTestApp();
  const response = await app.handle(new Request(`http://localhost${path}`, { method }));

  if (!response) {
    throw new Error("Expected middleware to produce a response");
  }

  return response;
}

Deno.test("authentication should allow login endpoint without authorization header", async () => {
  const response = await dispatch("/api/auth/login", "POST");

  assertEquals(response.status, 204);
});

Deno.test("authentication should allow prefixed login endpoint without authorization header", async () => {
  const response = await dispatch("/core/api/auth/login", "POST");

  assertEquals(response.status, 204);
});

Deno.test("authentication should allow OPTIONS requests without authorization header", async () => {
  const response = await dispatch("/api/feature/go", "OPTIONS");

  assertEquals(response.status, 204);
});

Deno.test("authentication should reject protected routes without authorization header", async () => {
  const response = await dispatch("/api/feature/go", "POST");
  const body = await response.json();

  assertEquals(response.status, 401);
  assertEquals(body.message, "Authorization token not provided");
  assertEquals(body.error, "MISSING_TOKEN");
});
