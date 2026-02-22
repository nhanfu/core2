import { describe, expect, test } from "bun:test";
import { hasPermission } from "../src/runtime/permission.js";

describe("hasPermission", () => {
  test("denies when no policies", () => {
    const allowed = hasPermission({ Name: "x", FeaturePolicies: [] }, undefined, { roleIds: ["ADMIN"] }, "read");
    expect(allowed).toBe(false);
  });

  test("allows matching role and CanWrite", () => {
    const allowed = hasPermission(
      { Name: "x", FeaturePolicies: [{ RoleId: "ADMIN", CanWrite: true }] },
      undefined,
      { roleIds: ["ADMIN"] },
      "write",
    );
    expect(allowed).toBe(true);
  });

  test("honors component deny", () => {
    const allowed = hasPermission(
      { Name: "x", FeaturePolicies: [{ RoleId: "ADMIN", CanWrite: true }] },
      { CanWrite: false },
      { roleIds: ["ADMIN"] },
      "write",
    );
    expect(allowed).toBe(false);
  });
});
