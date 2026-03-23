import { assertEquals, assertExists, assertRejects } from "jsr:@std/assert@1";

// Test crypto utilities
import { GenerateSalt, SHA256, HashPassword } from "./crypto.ts";
import {
  signToken,
  verifyToken,
  generateRefreshToken,
  isTokenExpired,
  getPayload,
  decodeToken,
  getTokenExpiration,
  getTokenIssuedAt,
  createUserTokenPayload,
  formatRoleClaims,
  ACCESS_TOKEN_EXPIRY,
  REFRESH_TOKEN_EXPIRY,
  ClaimTypes,
  signTokenWithOptions,
  verifyTokenWithOptions,
} from "./jwt.ts";

// ============================================
// Crypto Tests
// ============================================

Deno.test("GenerateSalt should generate a valid salt", () => {
  const salt = GenerateSalt();
  assertExists(salt);
  assertEquals(typeof salt, "string");
  assertEquals(salt.length > 0, true);
});

Deno.test("GenerateSalt should generate unique salts", () => {
  const salt1 = GenerateSalt();
  const salt2 = GenerateSalt();
  assertEquals(salt1 !== salt2, true);
});

Deno.test("SHA256 should compute hash correctly", async () => {
  const hash = await SHA256("test");
  assertEquals(typeof hash, "string");
  assertEquals(hash.length, 64); // SHA256 produces 64 hex characters
});

Deno.test("SHA256 should produce consistent output for same input", async () => {
  const hash1 = await SHA256("test");
  const hash2 = await SHA256("test");
  assertEquals(hash1, hash2);
});

Deno.test("SHA256 should produce different output for different inputs", async () => {
  const hash1 = await SHA256("test1");
  const hash2 = await SHA256("test2");
  assertEquals(hash1 !== hash2, true);
});

Deno.test("HashPassword should hash password with salt", async () => {
  const salt = GenerateSalt();
  const password = "testPassword123";
  const hash = await HashPassword(password, salt);
  assertExists(hash);
  assertEquals(typeof hash, "string");
  assertEquals(hash.length > 0, true);
});

Deno.test("HashPassword should produce different hashes for different salts", async () => {
  const password = "testPassword123";
  const salt1 = GenerateSalt();
  const salt2 = GenerateSalt();
  const hash1 = await HashPassword(password, salt1);
  const hash2 = await HashPassword(password, salt2);
  assertEquals(hash1 !== hash2, true);
});

// ============================================
// JWT Sign/Verify Tests
// ============================================

Deno.test("signToken should generate a valid JWT", async () => {
  const payload = { userId: "123", role: "admin" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  assertExists(token);
  assertEquals(typeof token, "string");
  assertEquals(token.split(".").length, 3);
});

Deno.test("signToken should use default expiry", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const decoded = getPayload(token);
  assertExists(decoded.exp);
});

Deno.test("signToken with custom expiry", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret, "1h");
  const decoded = getPayload(token);
  assertExists(decoded.exp);
});

Deno.test("verifyToken should verify valid JWT", async () => {
  const payload = { userId: "123", role: "admin" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const decoded = await verifyToken(token, secret);
  assertEquals(decoded?.userId, "123");
  assertEquals(decoded?.role, "admin");
});

Deno.test("verifyToken should reject invalid token", async () => {
  const secret = "test-secret-key";
  await assertRejects(
    () => verifyToken("invalid.token.here", secret),
    Error
  );
});

Deno.test("verifyToken should reject token with wrong secret", async () => {
  const payload = { userId: "123" };
  const secret1 = "test-secret-key-1";
  const secret2 = "test-secret-key-2";
  const token = await signToken(payload, secret1);
  await assertRejects(
    () => verifyToken(token, secret2),
    Error
  );
});

// ============================================
// JWT with Options Tests
// ============================================

Deno.test("signTokenWithOptions should sign with issuer", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signTokenWithOptions(payload, {
    secret,
    issuer: "test-issuer",
  });
  const decoded = await verifyTokenWithOptions(token, {
    secret,
    issuer: "test-issuer",
  });
  assertEquals(decoded?.iss, "test-issuer");
});

Deno.test("signTokenWithOptions should sign with audience", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signTokenWithOptions(payload, {
    secret,
    audience: "test-audience",
  });
  const decoded = await verifyTokenWithOptions(token, {
    secret,
    audience: "test-audience",
  });
  assertEquals(decoded?.aud, "test-audience");
});

Deno.test("signTokenWithOptions should generate JTI", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signTokenWithOptions(payload, { secret });
  const decoded = getPayload(token);
  assertExists(decoded.jti);
});

Deno.test("signTokenWithOptions should accept custom JTI", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const customJti = "custom-jti-123";
  const token = await signTokenWithOptions(payload, {
    secret,
    jti: customJti,
  });
  const decoded = getPayload(token);
  assertEquals(decoded.jti, customJti);
});

// ============================================
// Decode Token Tests
// ============================================

Deno.test("decodeToken should return header, payload, and signature", async () => {
  const payload = { userId: "123", role: "admin" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const decoded = decodeToken(token);
  assertExists(decoded.header);
  assertExists(decoded.payload);
  assertExists(decoded.signature);
  assertEquals(decoded.header.alg, "HS256");
  assertEquals(decoded.header.typ, "JWT");
  assertEquals(decoded.payload.userId, "123");
});

Deno.test("getPayload should decode token without verification", async () => {
  const payload = { userId: "123", role: "admin" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const decoded = getPayload(token);
  assertEquals(decoded?.userId, "123");
  assertEquals(decoded?.role, "admin");
});

// ============================================
// Token Expiration Tests
// ============================================

Deno.test("isTokenExpired should detect expired token", () => {
  const payload = {
    userId: "123",
    exp: Math.floor(Date.now() / 1000) - 3600
  };
  const expired = isTokenExpired(payload as any);
  assertEquals(expired, true);
});

Deno.test("isTokenExpired should return false for non-expired token", () => {
  const payload = {
    userId: "123",
    exp: Math.floor(Date.now() / 1000) + 3600
  };
  const expired = isTokenExpired(payload as any);
  assertEquals(expired, false);
});

Deno.test("isTokenExpired should return false for token without expiration", () => {
  const payload = { userId: "123" };
  const expired = isTokenExpired(payload as any);
  assertEquals(expired, false);
});

Deno.test("getTokenExpiration should return expiration timestamp", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const exp = getTokenExpiration(token);
  assertExists(exp);
  assertEquals(typeof exp, "number");
});

Deno.test("getTokenIssuedAt should return issued at timestamp", async () => {
  const payload = { userId: "123" };
  const secret = "test-secret-key";
  const token = await signToken(payload, secret);
  const iat = getTokenIssuedAt(token);
  assertExists(iat);
  assertEquals(typeof iat, "number");
});

// ============================================
// Refresh Token Tests
// ============================================

Deno.test("generateRefreshToken should generate a random token", () => {
  const token1 = generateRefreshToken();
  const token2 = generateRefreshToken();
  assertEquals(typeof token1, "string");
  assertEquals(token1.length, 32);
  assertEquals(token1 !== token2, true);
});

Deno.test("generateRefreshToken should generate custom length", () => {
  const token = generateRefreshToken(64);
  assertEquals(token.length, 64);
});

// ============================================
// User Token Payload Tests
// ============================================

Deno.test("createUserTokenPayload should create payload with required fields", () => {
  const payload = createUserTokenPayload("user1", "username", ["role1"], ["Admin"]);
  assertEquals(payload.userId, "user1");
  assertEquals(payload.userName, "username");
  assertEquals(payload.roleIds, ["role1"]);
  assertEquals(payload.roleNameClaim, ["Admin"]);
});

Deno.test("createUserTokenPayload should include additional claims", () => {
  const payload = createUserTokenPayload("user1", "username", ["role1"], ["Admin"], {
    email: "test@example.com",
    tenantCode: "tenant1",
  });
  assertEquals(payload.email, "test@example.com");
  assertEquals(payload.tenantCode, "tenant1");
});

Deno.test("createUserTokenPayload should handle empty roles", () => {
  const payload = createUserTokenPayload("user1", "username", [], []);
  assertEquals(payload.userId, "user1");
  assertEquals(payload.roleIds, undefined);
});

// ============================================
// Format Role Claims Tests
// ============================================

Deno.test("formatRoleClaims should format role IDs", () => {
  const result = formatRoleClaims(["role1", "role2", "role3"]);
  assertEquals(result.RoleIds, ["role1", "role2", "role3"]);
});

// ============================================
// Constants Tests
// ============================================

Deno.test("ACCESS_TOKEN_EXPIRY should be 1 day", () => {
  assertEquals(ACCESS_TOKEN_EXPIRY, "1d");
});

Deno.test("REFRESH_TOKEN_EXPIRY should be 365 days", () => {
  assertEquals(REFRESH_TOKEN_EXPIRY, "365d");
});

Deno.test("ClaimTypes should have all required claims", () => {
  assertEquals(ClaimTypes.USER_ID, "UserId");
  assertEquals(ClaimTypes.USER_NAME, "UserName");
  assertEquals(ClaimTypes.FULL_NAME, "FullName");
  assertEquals(ClaimTypes.EMAIL, "Email");
  assertEquals(ClaimTypes.TENANT_CLAIM, "TenantCode");
  assertEquals(ClaimTypes.ROLE_IDS, "RoleIds");
  assertEquals(ClaimTypes.ROLE_NAME_CLAIM, "RoleName");
});

