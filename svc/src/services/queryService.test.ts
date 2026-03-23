import { assertEquals, assertExists } from "jsr:@std/assert@1";

// Test queryService utilities - pure functions only
// We test createTokenContext and replaceTokens which don't require database

import type { UserContext } from "../types/interfaces.ts";

/** Token context interface */
interface TokenContext {
  TokenUserId: string;
  TokenUserName: string;
  TokenFullName: string;
  TokenEmail: string;
  TokenRoleIds: string;
  TokenRoleNames: string;
  TokenDepartmentId: string;
  TokenPositionId: string;
  TokenTeamId: string;
  TokenPartnerId: string;
  TokenCenterIds: string;
  TokenTenantCode: string;
  TokenEnv: string;
  TokenConnKey: string;
  [key: string]: string;
}

/**
 * Create a TokenContext from UserContext
 */
function createTokenContext(userContext?: UserContext): TokenContext {
  const defaultContext: TokenContext = {
    TokenUserId: "",
    TokenUserName: "",
    TokenFullName: "",
    TokenEmail: "",
    TokenRoleIds: "",
    TokenRoleNames: "",
    TokenDepartmentId: "",
    TokenPositionId: "",
    TokenTeamId: "",
    TokenPartnerId: "",
    TokenCenterIds: "",
    TokenTenantCode: "",
    TokenEnv: "",
    TokenConnKey: "",
  };

  if (!userContext) {
    return defaultContext;
  }

  return {
    TokenUserId: userContext.userId || "",
    TokenUserName: userContext.userName || "",
    TokenFullName: userContext.fullName || "",
    TokenEmail: userContext.email || "",
    TokenRoleIds: userContext.roles?.join(",") || "",
    TokenRoleNames: userContext.roleNames?.join(",") || "",
    TokenDepartmentId: userContext.departmentId || "",
    TokenPositionId: userContext.positionId || "",
    TokenTeamId: userContext.teamId || "",
    TokenPartnerId: userContext.partnerId || "",
    TokenCenterIds: userContext.centerIds?.join(",") || "",
    TokenTenantCode: userContext.tenantCode || "",
    TokenEnv: userContext.env || "",
    TokenConnKey: userContext.connKey || "",
  };
}

/**
 * Replace tokens in a query string
 */
function replaceTokens(sql: string, context: TokenContext): string {
  let result = sql;

  for (const [key, value] of Object.entries(context)) {
    const token = `{${key}}`;
    const replacement = value !== undefined && value !== null ? String(value) : "";
    result = result.split(token).join(replacement);
  }

  return result;
}

// ============================================
// Tests
// ============================================

Deno.test("createTokenContext should return default context when no userContext", () => {
  const context = createTokenContext();
  assertEquals(context.TokenUserId, "");
  assertEquals(context.TokenUserName, "");
  assertEquals(context.TokenFullName, "");
  assertEquals(context.TokenEmail, "");
  assertEquals(context.TokenRoleIds, "");
  assertEquals(context.TokenRoleNames, "");
  assertEquals(context.TokenDepartmentId, "");
  assertEquals(context.TokenPositionId, "");
  assertEquals(context.TokenTeamId, "");
  assertEquals(context.TokenPartnerId, "");
  assertEquals(context.TokenCenterIds, "");
  assertEquals(context.TokenTenantCode, "");
  assertEquals(context.TokenEnv, "");
  assertEquals(context.TokenConnKey, "");
});

Deno.test("createTokenContext should populate from userContext", () => {
  const userContext: UserContext = {
    userId: "user-123",
    userName: "testuser",
    fullName: "Test User",
    email: "test@example.com",
    roles: ["role1", "role2"],
    roleNames: ["Admin", "Editor"],
    tenantCode: "tenant-001",
    env: "development",
    connKey: "conn-001",
    departmentId: "dept-001",
    positionId: "pos-001",
    teamId: "team-001",
    partnerId: "partner-001",
    centerIds: ["center1", "center2"],
  };

  const context = createTokenContext(userContext);

  assertEquals(context.TokenUserId, "user-123");
  assertEquals(context.TokenUserName, "testuser");
  assertEquals(context.TokenFullName, "Test User");
  assertEquals(context.TokenEmail, "test@example.com");
  assertEquals(context.TokenRoleIds, "role1,role2");
  assertEquals(context.TokenRoleNames, "Admin,Editor");
  assertEquals(context.TokenDepartmentId, "dept-001");
  assertEquals(context.TokenPositionId, "pos-001");
  assertEquals(context.TokenTeamId, "team-001");
  assertEquals(context.TokenPartnerId, "partner-001");
  assertEquals(context.TokenCenterIds, "center1,center2");
  assertEquals(context.TokenTenantCode, "tenant-001");
  assertEquals(context.TokenEnv, "development");
  assertEquals(context.TokenConnKey, "conn-001");
});

Deno.test("createTokenContext should handle missing optional fields", () => {
  const userContext: UserContext = {
    userId: "user-123",
    userName: "testuser",
    fullName: "Test User",
    email: "test@example.com",
    roles: [],
    roleNames: [],
    tenantCode: "tenant-001",
    env: "development",
    connKey: "conn-001",
  };

  const context = createTokenContext(userContext);

  assertEquals(context.TokenUserId, "user-123");
  assertEquals(context.TokenRoleIds, "");
  assertEquals(context.TokenRoleNames, "");
  assertEquals(context.TokenDepartmentId, "");
  assertEquals(context.TokenCenterIds, "");
});

Deno.test("replaceTokens should replace all tokens in query", () => {
  const context: TokenContext = {
    TokenUserId: "user-123",
    TokenUserName: "testuser",
    TokenFullName: "Test User",
    TokenEmail: "test@example.com",
    TokenRoleIds: "role1,role2",
    TokenRoleNames: "Admin",
    TokenDepartmentId: "dept-001",
    TokenPositionId: "pos-001",
    TokenTeamId: "team-001",
    TokenPartnerId: "partner-001",
    TokenCenterIds: "center1",
    TokenTenantCode: "tenant-001",
    TokenEnv: "dev",
    TokenConnKey: "conn-001",
  };

  const sql = "SELECT * FROM users WHERE user_id = '{TokenUserId}' AND tenant = '{TokenTenantCode}'";
  const result = replaceTokens(sql, context);

  assertEquals(result, "SELECT * FROM users WHERE user_id = 'user-123' AND tenant = 'tenant-001'");
});

Deno.test("replaceTokens should handle empty values", () => {
  const context: TokenContext = {
    TokenUserId: "",
    TokenUserName: "",
    TokenFullName: "",
    TokenEmail: "",
    TokenRoleIds: "",
    TokenRoleNames: "",
    TokenDepartmentId: "",
    TokenPositionId: "",
    TokenTeamId: "",
    TokenPartnerId: "",
    TokenCenterIds: "",
    TokenTenantCode: "",
    TokenEnv: "",
    TokenConnKey: "",
  };

  const sql = "SELECT * FROM users WHERE user_id = '{TokenUserId}' AND tenant = '{TokenTenantCode}'";
  const result = replaceTokens(sql, context);

  assertEquals(result, "SELECT * FROM users WHERE user_id = '' AND tenant = ''");
});

Deno.test("replaceTokens should handle missing tokens", () => {
  const context: TokenContext = {
    TokenUserId: "user-123",
    TokenUserName: "testuser",
    TokenFullName: "Test User",
    TokenEmail: "test@example.com",
    TokenRoleIds: "role1",
    TokenRoleNames: "Admin",
    TokenDepartmentId: "dept-001",
    TokenPositionId: "pos-001",
    TokenTeamId: "team-001",
    TokenPartnerId: "partner-001",
    TokenCenterIds: "center1",
    TokenTenantCode: "tenant-001",
    TokenEnv: "dev",
    TokenConnKey: "conn-001",
  };

  const sql = "SELECT * FROM users WHERE id = '{UnknownToken}'";
  const result = replaceTokens(sql, context);

  // Unknown tokens should remain unchanged
  assertEquals(result, "SELECT * FROM users WHERE id = '{UnknownToken}'");
});

Deno.test("replaceTokens should handle multiple occurrences of same token", () => {
  const context: TokenContext = {
    TokenUserId: "user-123",
    TokenUserName: "testuser",
    TokenFullName: "Test User",
    TokenEmail: "test@example.com",
    TokenRoleIds: "role1",
    TokenRoleNames: "Admin",
    TokenDepartmentId: "dept-001",
    TokenPositionId: "pos-001",
    TokenTeamId: "team-001",
    TokenPartnerId: "partner-001",
    TokenCenterIds: "center1",
    TokenTenantCode: "tenant-001",
    TokenEnv: "dev",
    TokenConnKey: "conn-001",
  };

  const sql = "SELECT * FROM users WHERE created_by = '{TokenUserId}' AND updated_by = '{TokenUserId}'";
  const result = replaceTokens(sql, context);

  assertEquals(result, "SELECT * FROM users WHERE created_by = 'user-123' AND updated_by = 'user-123'");
});

Deno.test("replaceTokens should handle custom tokens", () => {
  const context: TokenContext = {
    TokenUserId: "user-123",
    TokenUserName: "testuser",
    TokenFullName: "Test User",
    TokenEmail: "test@example.com",
    TokenRoleIds: "role1",
    TokenRoleNames: "Admin",
    TokenDepartmentId: "dept-001",
    TokenPositionId: "pos-001",
    TokenTeamId: "team-001",
    TokenPartnerId: "partner-001",
    TokenCenterIds: "center1",
    TokenTenantCode: "tenant-001",
    TokenEnv: "dev",
    TokenConnKey: "conn-001",
    CustomToken: "custom-value",
    AnotherToken: "another-value",
  };

  const sql = "SELECT * FROM table WHERE custom = '{CustomToken}' AND another = '{AnotherToken}'";
  const result = replaceTokens(sql, context);

  assertEquals(result, "SELECT * FROM table WHERE custom = 'custom-value' AND another = 'another-value'");
});

Deno.test("replaceTokens should handle special characters in values", () => {
  const context: TokenContext = {
    TokenUserId: "user-123",
    TokenUserName: "testuser",
    TokenFullName: "Test User",
    TokenEmail: "test@example.com",
    TokenRoleIds: "role1",
    TokenRoleNames: "Admin",
    TokenDepartmentId: "dept-001",
    TokenPositionId: "pos-001",
    TokenTeamId: "team-001",
    TokenPartnerId: "partner-001",
    TokenCenterIds: "center1",
    TokenTenantCode: "tenant-001",
    TokenEnv: "dev",
    TokenConnKey: "conn-001",
    SearchTerm: "O'Brien & Sons",
  };

  const sql = "SELECT * FROM users WHERE name = '{SearchTerm}'";
  const result = replaceTokens(sql, context);

  assertEquals(result, "SELECT * FROM users WHERE name = 'O'Brien & Sons'");
});

Deno.test("createTokenContext should handle isAdmin field", () => {
  const userContext: UserContext = {
    userId: "user-123",
    userName: "testuser",
    fullName: "Test User",
    email: "test@example.com",
    roles: ["admin"],
    roleNames: ["Admin"],
    tenantCode: "tenant-001",
    env: "development",
    connKey: "conn-001",
    isAdmin: true,
  };

  const context = createTokenContext(userContext);

  assertEquals(context.TokenUserId, "user-123");
  assertEquals(context.TokenUserName, "testuser");
});

