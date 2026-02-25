import { assertEquals } from "https://deno.land/std@0.208.0/assert/mod.ts";
import { hasPermission } from "../src/runtime/permission.ts";

Deno.test("hasPermission: denies when no policies", () => {
  const allowed = hasPermission({ Name: "x", FeaturePolicies: [] }, undefined, { roleIds: ["ADMIN"] }, "read");
  assertEquals(allowed, false);
});

Deno.test("hasPermission: allows matching role and CanWrite", () => {
  const allowed = hasPermission(
    { Name: "x", FeaturePolicies: [{ RoleId: "ADMIN", CanWrite: true }] },
    undefined,
    { roleIds: ["ADMIN"] },
    "write",
  );
  assertEquals(allowed, true);
});

Deno.test("hasPermission: honors component deny", () => {
  const allowed = hasPermission(
    { Name: "x", FeaturePolicies: [{ RoleId: "ADMIN", CanWrite: true }] },
    { CanWrite: false },
    { roleIds: ["ADMIN"] },
    "write",
  );
  assertEquals(allowed, false);
});
