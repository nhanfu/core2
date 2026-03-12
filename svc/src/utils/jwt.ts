/**
 * JWT Utilities for Deno
 * Compatible with .NET JWT tokens using HMAC-SHA256
 */
import {
  SignJWT,
  jwtVerify,
  decodeJwt,
  decodeProtectedHeader,
  type JWTPayload,
  type JWSHeaderParameters,
} from "https://deno.land/x/jose@v5.0.0/index.ts";

// Default token expiry times
export const ACCESS_TOKEN_EXPIRY = "1d"; // 1 day
export const REFRESH_TOKEN_EXPIRY = "365d"; // 1 year

// Standard JWT claim names (matching .NET conventions)
export const ClaimTypes = {
  USER_ID: "UserId",
  USER_NAME: "UserName",
  FULL_NAME: "FullName",
  EMAIL: "Email",
  TENANT_CLAIM: "TenantCode", // Maps to "TenantClaim" in .NET
  ROLE_IDS: "RoleIds",
  ROLE_NAME_CLAIM: "RoleName", // Maps to "RoleNameClaim" in .NET
  PARTNER_ID: "PartnerId",
  AVATAR: "Avatar",
  TEAM_ID: "TeamId",
  DEPARTMENT_ID: "DepartmentId",
  COMPANY_NAME: "CName",
  COMPANY_LOGO: "CLogo",
  COMPANY_ICON: "CIcon",
  COMPANY_ADDRESS: "CAddress",
  COMPANY_PHONE: "CPhoneNumber",
  COMPANY_EMAIL: "CEmail",
  DOB: "Dob",
} as const;

/**
 * Extended JWT payload interface with application-specific claims
 */
export interface TokenPayload extends JWTPayload {
  // Standard claims (required by .NET compatibility)
  iss?: string; // Issuer
  sub?: string; // Subject
  aud?: string | string[]; // Audience
  exp?: number; // Expiration Time
  nbf?: number; // Not Before
  iat?: number; // Issued At
  jti?: string; // JWT ID

  // Application-specific claims (matching .NET claims)
  userId?: string;
  userName?: string;
  fullName?: string;
  email?: string;
  tenantCode?: string;
  tenantClaim?: string; // .NET uses this name
  roleIds?: string | string[];
  roleName?: string | string[];
  roleNameClaim?: string | string[]; // .NET uses this name
  partnerId?: string;
  avatar?: string;
  teamId?: string;
  departmentId?: string;
  cName?: string;
  cLogo?: string;
  cIcon?: string;
  cAddress?: string;
  cPhoneNumber?: string;
  cEmail?: string;
  Dob?: string;
}

/**
 * Options for signing JWT tokens
 */
export interface SignTokenOptions {
  /** Secret key for signing (must be consistent with .NET Tokens:Key) */
  secret: string;
  /** Token expiration string (e.g., "1d", "365d", "1h") */
  expiresIn?: string;
  /** Token issuer (must match .NET Tokens:Issuer) */
  issuer?: string;
  /** Token audience */
  audience?: string;
  /** JWT ID (session ID) */
  jti?: string;
}

/**
 * Options for verifying JWT tokens
 */
export interface VerifyTokenOptions {
  /** Secret key for verification (must match signing secret) */
  secret: string;
  /** Expected token issuer */
  issuer?: string;
  /** Expected token audience */
  audience?: string;
}

/**
 * Decoded token header information
 */
export interface TokenHeader {
  alg: string;
  typ: string;
  [key: string]: unknown;
}

/**
 * Result from decodeToken function
 */
export interface DecodedToken {
  header: TokenHeader;
  payload: TokenPayload;
  signature: string;
}

/**
 * Signs a JWT token with the specified payload
 * Compatible with .NET HMAC-SHA256 tokens
 *
 * @param payload - The claims/payload to include in the token
 * @param secret - The secret key for signing
 * @param expiresIn - Token expiration (default: "1d" for 1 day)
 * @returns The signed JWT token string
 */
export async function signToken(
  payload: object,
  secret: string,
  expiresIn: string = ACCESS_TOKEN_EXPIRY
): Promise<string> {
  const jwt = await new SignJWT(payload as JWTPayload)
    .setProtectedHeader({ alg: "HS256", typ: "JWT" })
    .setIssuedAt()
    .setExpirationTime(expiresIn);

  // Add JTI if not present (for session tracking)
  if (!("jti" in payload)) {
    // Generate a unique ID for the token
    const jti = crypto.randomUUID();
    jwt.setJti(jti);
  }

  return await jwt.sign(new TextEncoder().encode(secret));
}

/**
 * Signs a JWT token with full options
 *
 * @param payload - The claims/payload to include in the token
 * @param options - Signing options including secret and other parameters
 * @returns The signed JWT token string
 */
export async function signTokenWithOptions(
  payload: object,
  options: SignTokenOptions
): Promise<string> {
  const { secret, expiresIn = ACCESS_TOKEN_EXPIRY, issuer, audience, jti } = options;

  const jwt = await new SignJWT(payload as JWTPayload)
    .setProtectedHeader({ alg: "HS256", typ: "JWT" })
    .setIssuedAt()
    .setExpirationTime(expiresIn);

  if (jti) {
    jwt.setJti(jti);
  } else if (!("jti" in payload)) {
    jwt.setJti(crypto.randomUUID());
  }

  if (issuer) {
    jwt.setIssuer(issuer);
  }

  if (audience) {
    jwt.setAudience(audience);
  }

  return await jwt.sign(new TextEncoder().encode(secret));
}

/**
 * Verifies and decodes a JWT token
 * Throws an error if the token is invalid or expired
 *
 * @param token - The JWT token string to verify
 * @param secret - The secret key for verification
 * @returns The decoded token payload if valid
 * @throws Error if token is invalid, expired, or signature doesn't match
 */
export async function verifyToken(
  token: string,
  secret: string
): Promise<TokenPayload> {
  const { payload } = await jwtVerify(
    token,
    new TextEncoder().encode(secret),
    {
      algorithms: ["HS256"],
    }
  );

  return payload as TokenPayload;
}

/**
 * Verifies and decodes a JWT token with additional options
 *
 * @param token - The JWT token string to verify
 * @param options - Verification options
 * @returns The decoded token payload if valid
 * @throws Error if token is invalid, expired, or signature doesn't match
 */
export async function verifyTokenWithOptions(
  token: string,
  options: VerifyTokenOptions
): Promise<TokenPayload> {
  const { secret, issuer, audience } = options;

  const { payload } = await jwtVerify(
    token,
    new TextEncoder().encode(secret),
    {
      algorithms: ["HS256"],
      issuer,
      audience,
    }
  );

  return payload as TokenPayload;
}

/**
 * Decodes a JWT token without verifying its signature
 * Useful for inspecting token contents without validation
 *
 * @param token - The JWT token string to decode
 * @returns Object containing header, payload, and signature
 * @throws Error if the token format is invalid
 */
export function decodeToken(token: string): DecodedToken {
  // Use jose's decodeJwt for the payload
  const payload = decodeJwt(token);

  // Get the protected header
  const header = decodeProtectedHeader(token) as TokenHeader;

  // Extract signature from the token
  const parts = token.split(".");
  const signature = parts.length === 3 ? parts[2] : "";

  return {
    header,
    payload: payload as TokenPayload,
    signature,
  };
}

/**
 * Extracts just the payload from a token without verification
 * Simplified version of decodeToken
 *
 * @param token - The JWT token string
 * @returns The decoded payload
 */
export function getPayload(token: string): TokenPayload {
  return decodeJwt(token) as TokenPayload;
}

/**
 * Checks if a token is expired
 *
 * @param token - The JWT token string or TokenPayload
 * @returns true if the token is expired, false otherwise
 */
export function isTokenExpired(token: string | TokenPayload): boolean {
  const payload = typeof token === "string" ? decodeJwt(token) : token;

  if (!payload.exp) {
    return false; // No expiration claim means never expires
  }

  const now = Math.floor(Date.now() / 1000);
  return payload.exp < now;
}

/**
 * Gets the expiration timestamp from a token
 *
 * @param token - The JWT token string
 * @returns The expiration timestamp (Unix epoch) or undefined if not set
 */
export function getTokenExpiration(token: string): number | undefined {
  const payload = decodeJwt(token);
  return payload.exp;
}

/**
 * Gets the issued-at timestamp from a token
 *
 * @param token - The JWT token string
 * @returns The issued-at timestamp (Unix epoch) or undefined if not set
 */
export function getTokenIssuedAt(token: string): number | undefined {
  const payload = decodeJwt(token);
  return payload.iat;
}

/**
 * Creates a user token payload compatible with .NET
 * Includes all standard claims used by the backend
 *
 * @param userId - The user's unique identifier
 * @param userName - The user's username
 * @param roleIds - Array of role IDs
 * @param roleNames - Array of role names
 * @param additionalClaims - Additional claims to include
 * @returns Token payload object
 */
export function createUserTokenPayload(
  userId: string,
  userName: string,
  roleIds: string[],
  roleNames: string[],
  additionalClaims: Partial<TokenPayload> = {}
): TokenPayload {
  const payload: TokenPayload = {
    ...additionalClaims,
    userId: userId,
    userName: userName,
  };

  // Add role IDs (multiple values as array)
  if (roleIds.length > 0) {
    payload.roleIds = roleIds;
  }

  // Add role names (multiple values as array)
  if (roleNames.length > 0) {
    payload.roleNameClaim = roleNames;
  }

  return payload;
}

/**
 * Generates an access token for a user
 *
 * @param userId - User's unique identifier
 * @param userName - User's username
 * @param roleIds - User's role IDs
 * @param roleNames - User's role names
 * @param secret - Signing secret
 * @param options - Additional token options
 * @returns Signed access token
 */
export async function generateAccessToken(
  userId: string,
  userName: string,
  roleIds: string[],
  roleNames: string[],
  secret: string,
  options: {
    issuer?: string;
    audience?: string;
    tenantCode?: string;
    partnerId?: string;
    fullName?: string;
    email?: string;
    avatar?: string;
    teamId?: string;
    departmentId?: string;
    jti?: string;
  } = {}
): Promise<string> {
  const payload = createUserTokenPayload(
    userId,
    userName,
    roleIds,
    roleNames,
    {
      tenantCode: options.tenantCode,
      tenantClaim: options.tenantCode,
      partnerId: options.partnerId,
      fullName: options.fullName,
      email: options.email,
      avatar: options.avatar,
      teamId: options.teamId,
      departmentId: options.departmentId,
    }
  );

  return await signTokenWithOptions(payload, {
    secret,
    expiresIn: ACCESS_TOKEN_EXPIRY,
    issuer: options.issuer,
    audience: options.audience,
    jti: options.jti,
  });
}

/**
 * Creates a refresh token (simple random string, not JWT)
 * The refresh token is stored in the database and validated separately
 *
 * @param length - Length of the refresh token (default: 32)
 * @returns A random refresh token string
 */
export function generateRefreshToken(length: number = 32): string {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  let result = "";
  const values = new Uint8Array(length);
  crypto.getRandomValues(values);

  for (let i = 0; i < length; i++) {
    result += chars[values[i] % chars.length];
  }

  return result;
}

/**
 * Utility to convert role arrays to claim format used by .NET
 * .NET adds multiple claims with the same type for array values
 *
 * @param roleIds - Array of role IDs
 * @returns Object with RoleIds as array
 */
export function formatRoleClaims(
  roleIds: string[]
): { RoleIds: string[] } {
  return {
    RoleIds: roleIds,
  };
}

// Re-export types for external use
export type {
  JWTPayload,
  JWSHeaderParameters,
};
