/**
 * Authentication Service
 * Handles user authentication, token generation, and refresh
 * Compatible with CoreAPI's AuthService.cs
 */

import { query, execute, insert, update } from "../database/postgresClient.ts";
import { HashPassword, SHA256 } from "../utils/crypto.ts";
import {
  generateAccessToken,
  generateRefreshToken,
  verifyToken,
  getPayload,
  ACCESS_TOKEN_EXPIRY,
  REFRESH_TOKEN_EXPIRY,
} from "../utils/jwt.ts";
import type { User, Token, UserLogin, Partner } from "../types/interfaces.ts";

// Token configuration from environment
const JWT_SECRET = Deno.env.get("JWT_SECRET") || "your-secret-key";
const JWT_ISSUER = Deno.env.get("JWT_ISSUER") || "CoreAPI";
const JWT_AUDIENCE = Deno.env.get("JWT_AUDIENCE") || "CoreAPI";

const USER_SELECT = `
  SELECT
    u."Id" AS id,
    u."Code" AS code,
    u."Email" AS email,
    u."Password" AS password,
    u."Salt" AS salt,
    u."CompanyId" AS "companyId",
    u."UserName" AS "userName",
    u."FullName" AS "fullName",
    u."Address" AS address,
    u."Avatar" AS avatar,
    u."Ssn" AS ssn,
    u."PhoneNumber" AS "phoneNumber",
    u."TeamId" AS "teamId",
    u."PartnerId" AS "partnerId",
    u."Active" AS active,
    u."DepartmentId" AS "departmentId",
    u."PositionId" AS "positionId",
    u."RoleIds" AS "roleIds",
    u."LoginFailedCount" AS "loginFailedCount",
    u."LastFailedLogin" AS "lastFailedLogin",
    u."LastLogin" AS "lastLogin",
    p."Code" AS tenant_code,
    p."Name" AS tenant_name
`;

/**
 * Get expiration date based on expiry string
 * @param expiresIn - Expiry string like "1d", "365d"
 * @returns Date object
 */
function getExpirationDate(expiresIn: string): Date {
  const now = new Date();
  const match = expiresIn.match(/^(\d+)([dhms])$/);
  if (!match) {
    // Default to 1 day
    now.setDate(now.getDate() + 1);
    return now;
  }

  const value = parseInt(match[1], 10);
  const unit = match[2];

  switch (unit) {
    case "d":
      now.setDate(now.getDate() + value);
      break;
    case "h":
      now.setHours(now.getHours() + value);
      break;
    case "m":
      now.setMinutes(now.getMinutes() + value);
      break;
    case "s":
      now.setSeconds(now.getSeconds() + value);
      break;
  }

  return now;
}

/**
 * Get role IDs for a user from user_roles table or roleIds field
 * @param userId - The user ID
 * @returns Array of role IDs
 */
async function getUserRoleIds(userId: string): Promise<string[]> {
  try {
    // Try to get from the join table first.
    const roles = await query(
      `SELECT "RoleId" AS "roleId"
       FROM "UserRole"
       WHERE "UserId" = $1 AND "Active" = true`,
      [userId]
    );

    if (roles && roles.length > 0) {
      return roles.map((r: any) => r.roleId);
    }

    // Fallback: use the denormalized RoleIds field on the user row.
    const users = await query(
      `SELECT "RoleIds" AS "roleIds"
       FROM "User"
       WHERE "Id" = $1`,
      [userId]
    );

    if (users && users.length > 0 && users[0].roleIds) {
      const roleIdsStr = users[0].roleIds;
      if (typeof roleIdsStr === "string") {
        return roleIdsStr.split(",").map((id: string) => id.trim()).filter(Boolean);
      }
      return [];
    }

    return [];
  } catch (error) {
    console.error("Error getting user role IDs:", error);
    return [];
  }
}

/**
 * Get role names for given role IDs
 * @param roleIds - Array of role IDs
 * @returns Array of role names
 */
async function getRoleNames(roleIds: string[]): Promise<string[]> {
  if (!roleIds || roleIds.length === 0) {
    return [];
  }

  try {
    const placeholders = roleIds.map((_, i) => `$${i + 1}`).join(", ");
    const roles = await query(
      `SELECT "Name" AS name
       FROM "Role"
       WHERE "Id" IN (${placeholders}) AND "Active" = true`,
      roleIds
    );

    return roles.map((r: any) => r.name);
  } catch (error) {
    console.error("Error getting role names:", error);
    return [];
  }
}

/**
 * Get user center IDs (departments/centers user belongs to)
 * @param userId - The user ID
 * @returns Array of center IDs
 */
async function getUserCenterIds(userId: string): Promise<string[]> {
  try {
    const userCenters = await query(
      `SELECT center_id FROM user_centers WHERE user_id = $1 AND active = true`,
      [userId]
    );

    if (userCenters && userCenters.length > 0) {
      return userCenters.map((uc: any) => uc.center_id);
    }

    return [];
  } catch (error) {
    console.error("Error getting user center IDs:", error);
    return [];
  }
}

/**
 * Get tenant/company information
 * @param tenantCode - The tenant code
 * @returns Partner object
 */
async function getTenant(tenantCode: string): Promise<Partner | null> {
  if (!tenantCode) {
    return null;
  }

  try {
    const partners = await query(
      `SELECT
         "Id" AS id,
         "Name" AS name,
         "Code" AS code,
         "Address" AS address,
         "PhoneNumber" AS "phoneNumber",
         "Mail" AS mail,
         "Email" AS email,
         "Active" AS active,
         "IsPublic" AS "isPublic",
         "IsNoDebt" AS "isNoDebt"
       FROM "Partner"
       WHERE "Code" = $1 AND "Active" = true`,
      [tenantCode]
    );

    if (partners && partners.length > 0) {
      return partners[0] as Partner;
    }

    return null;
  } catch (error) {
    console.error("Error getting tenant:", error);
    return null;
  }
}

/**
 * Sign in a user with username and password
 * @param userName - The username
 * @param password - The plain text password
 * @returns Token object with user data and tokens
 * @throws Error if credentials are invalid
 */
export async function SignIn(
  userName: string,
  password: string
): Promise<Token> {
  const users = await query(
    `${USER_SELECT}
     FROM "User" u
     LEFT JOIN "Partner" p ON u."CompanyId" = p."Id"
     WHERE u."UserName" = $1 AND u."Active" = true`,
    [userName]
  );

  if (!users || users.length === 0) {
    throw new Error("Invalid username or password");
  }

  const user = {
    ...(users[0] as User & { tenant_code?: string }),
    tenant_code: (users[0] as { tenant_code?: string }).tenant_code ||
      Deno.env.get("TENANT_CODE") || "system",
  };

  // Check if user is active
  if (!user.active) {
    throw new Error("User account is inactive");
  }

  // Step 2: Hash provided password with user's salt
  if (!user.salt) {
    throw new Error("User has no salt configured");
  }

  const hashedPassword = await HashPassword(password, user.salt);

  // Step 3: Compare with stored password
  if (hashedPassword !== user.password) {
    // Increment failed login count
    await execute(
      `UPDATE "User"
       SET "LoginFailedCount" = COALESCE("LoginFailedCount", 0) + 1,
           "LastFailedLogin" = NOW()
       WHERE "Id" = $1`,
      [user.id]
    );
    throw new Error("Invalid username or password");
  }

  // Step 4: Generate access and refresh tokens
  const accessToken = await GenerateAccessToken(user);
  const refreshToken = GenerateRefreshToken();

  // Calculate expiration dates
  const accessTokenExp = getExpirationDate(ACCESS_TOKEN_EXPIRY);
  const refreshTokenExp = getExpirationDate(REFRESH_TOKEN_EXPIRY);

  // Get additional user data
  const roleIds = await getUserRoleIds(user.id);
  const roleNames = await getRoleNames(roleIds);
  const centerIds = await getUserCenterIds(user.id);

  // Get tenant/company info
  const tenant = await getTenant(user.tenant_code || "");

  // Step 5: Store refresh token in database
  try {
    await insert("\"UserLogin\"", {
      "\"Id\"": crypto.randomUUID(),
      "\"UserId\"": user.id,
      "\"AccessToken\"": accessToken,
      "\"RefreshToken\"": refreshToken,
      "\"AccessTokenExp\"": accessTokenExp.toISOString(),
      "\"RefreshTokenExp\"": refreshTokenExp.toISOString(),
      "\"Active\"": true,
      "\"InsertedDate\"": new Date().toISOString(),
      "\"InsertedBy\"": user.id,
    });

    // Update last login
    await execute(
      `UPDATE "User"
       SET "LastLogin" = NOW(), "LoginFailedCount" = 0
       WHERE "Id" = $1`,
      [user.id]
    );
  } catch (error) {
    console.error("Error storing login record:", error);
    // Continue even if login record fails
  }

  // Step 6: Return Token object
  return {
    userId: user.id,
    userName: user.userName,
    email: user.email || "",
    fullName: user.fullName || "",
    departmentId: user.departmentId || "",
    code: user.code || "",
    positionId: user.positionId || "",
    address: user.address || "",
    avatar: user.avatar || "",
    accessToken,
    refreshToken,
    accessTokenExp,
    refreshTokenExp,
    vendor: tenant || undefined,
    roleIds,
    roleNames,
    centerIds,
    ssn: user.ssn || "",
    phoneNumber: user.phoneNumber || "",
    teamId: user.teamId || "",
    partnerId: user.partnerId || "",
    regionId: "",
    signinDate: new Date(),
    tenantCode: user.tenant_code || "system",
    env: Deno.env.get("ENV") || "production",
    connKey: Deno.env.get("CONN_KEY") || "default",
  };
}

/**
 * Refresh an access token using a refresh token
 * @param refreshToken - The refresh token
 * @returns New Token object with updated tokens
 * @throws Error if refresh token is invalid
 */
export async function RefreshToken(refreshToken: string): Promise<Token> {
  // Step 1: Find the login record with this refresh token
  const loginRecords = await query(
    `SELECT
       ul."UserId" AS "userId",
       ul."Active" AS active,
       ${USER_SELECT.replace(/^(\s*SELECT\s*)/m, "")}
     FROM "UserLogin" ul
     JOIN "User" u ON ul."UserId" = u."Id"
     LEFT JOIN "Partner" p ON u."CompanyId" = p."Id"
     WHERE ul."RefreshToken" = $1 AND ul."Active" = true
       AND ul."RefreshTokenExp" > NOW()`,
    [refreshToken]
  );

  if (!loginRecords || loginRecords.length === 0) {
    throw new Error("Invalid or expired refresh token");
  }

  const loginRecord = loginRecords[0];

  // Step 2: Check if user is still active
  if (!loginRecord.active) {
    throw new Error("User account is inactive");
  }

  // Step 3: Get fresh user data
  const users = await query(
    `${USER_SELECT}
     FROM "User" u
     LEFT JOIN "Partner" p ON u."CompanyId" = p."Id"
     WHERE u."Id" = $1 AND u."Active" = true`,
    [loginRecord.userId]
  );

  if (!users || users.length === 0) {
    throw new Error("User not found or inactive");
  }

  const user = users[0] as User & { tenant_code?: string };

  // Step 4: Generate new access token
  const newAccessToken = await GenerateAccessToken(user);
  const newRefreshToken = GenerateRefreshToken();

  // Calculate new expiration dates
  const newAccessTokenExp = getExpirationDate(ACCESS_TOKEN_EXPIRY);
  const newRefreshTokenExp = getExpirationDate(REFRESH_TOKEN_EXPIRY);

  // Get role and center info
  const roleIds = await getUserRoleIds(user.id);
  const roleNames = await getRoleNames(roleIds);
  const centerIds = await getUserCenterIds(user.id);

  // Get tenant/company info
  const tenant = user.tenant_code ? await getTenant(user.tenant_code) : null;

  // Step 5: Deactivate old login record and create new one
  try {
    await execute(
      `UPDATE "UserLogin"
       SET "Active" = false, "UpdatedDate" = NOW()
       WHERE "RefreshToken" = $1`,
      [refreshToken]
    );

    await insert("\"UserLogin\"", {
      "\"Id\"": crypto.randomUUID(),
      "\"UserId\"": user.id,
      "\"AccessToken\"": newAccessToken,
      "\"RefreshToken\"": newRefreshToken,
      "\"AccessTokenExp\"": newAccessTokenExp.toISOString(),
      "\"RefreshTokenExp\"": newRefreshTokenExp.toISOString(),
      "\"Active\"": true,
      "\"InsertedDate\"": new Date().toISOString(),
      "\"InsertedBy\"": user.id,
    });
  } catch (error) {
    console.error("Error updating login record:", error);
    // Continue even if update fails
  }

  // Step 6: Return new Token object
  return {
    userId: user.id,
    userName: user.userName,
    email: user.email || "",
    fullName: user.fullName || "",
    departmentId: user.departmentId || "",
    code: user.code || "",
    positionId: user.positionId || "",
    address: user.address || "",
    avatar: user.avatar || "",
    accessToken: newAccessToken,
    refreshToken: newRefreshToken,
    accessTokenExp: newAccessTokenExp,
    refreshTokenExp: newRefreshTokenExp,
    vendor: tenant || undefined,
    roleIds,
    roleNames,
    centerIds,
    ssn: user.ssn || "",
    phoneNumber: user.phoneNumber || "",
    teamId: user.teamId || "",
    partnerId: user.partnerId || "",
    regionId: "",
    signinDate: new Date(),
    tenantCode: user.tenant_code || "",
    env: Deno.env.get("ENV") || "production",
    connKey: Deno.env.get("CONN_KEY") || "default",
  };
}

/**
 * Generate an access token for a user
 * Creates JWT token with claims (UserId, RoleIds, TenantCode, etc.)
 * @param user - The user object
 * @returns JWT access token string
 */
export async function GenerateAccessToken(user: User & { tenant_code?: string }): Promise<string> {
  // Get role IDs and role names
  const roleIds = await getUserRoleIds(user.id);
  const roleNames = await getRoleNames(roleIds);

  return await generateAccessToken(
    user.id,
    user.userName,
    roleIds,
    roleNames,
    JWT_SECRET,
    {
      issuer: JWT_ISSUER,
      audience: JWT_AUDIENCE,
      tenantCode: (user as any).tenant_code || user.companyId,
      partnerId: user.companyId,
      fullName: user.fullName,
      email: user.email,
      avatar: user.avatar,
      teamId: user.teamId,
      departmentId: user.departmentId,
    }
  );
}

/**
 * Generate a refresh token
 * @returns Random refresh token string
 */
export function GenerateRefreshToken(): string {
  return generateRefreshToken(32);
}

/**
 * Validate an access token and return the user context
 * @param token - The access token to validate
 * @returns Token payload if valid
 * @throws Error if token is invalid
 */
export async function ValidateAccessToken(token: string): Promise<any> {
  try {
    const payload = await verifyToken(token, JWT_SECRET);

    // Verify issuer and audience if configured
    if (JWT_ISSUER && payload.iss !== JWT_ISSUER) {
      throw new Error("Invalid token issuer");
    }

    if (JWT_AUDIENCE) {
      const aud = payload.aud;
      if (!aud || (Array.isArray(aud) && !aud.includes(JWT_AUDIENCE))) {
        throw new Error("Invalid token audience");
      }
    }

    return payload;
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Token validation failed: ${error.message}`);
    }
    throw new Error("Token validation failed");
  }
}

/**
 * Sign out a user by invalidating their refresh token
 * @param refreshToken - The refresh token to invalidate
 */
export async function SignOut(refreshToken: string): Promise<void> {
  try {
    await execute(
      `UPDATE "UserLogin"
       SET "Active" = false, "UpdatedDate" = NOW()
       WHERE "RefreshToken" = $1`,
      [refreshToken]
    );
  } catch (error) {
    console.error("Error signing out:", error);
  }
}

/**
 * Change user password
 * @param userId - The user ID
 * @param currentPassword - The current password
 * @param newPassword - The new password
 * @returns True if password was changed successfully
 * @throws Error if current password is invalid
 */
export async function ChangePassword(
  userId: string,
  currentPassword: string,
  newPassword: string
): Promise<boolean> {
  // Get current user
  const users = await query(
    `${USER_SELECT}
     FROM "User" u
     LEFT JOIN "Partner" p ON u."CompanyId" = p."Id"
     WHERE u."Id" = $1 AND u."Active" = true`,
    [userId]
  );

  if (!users || users.length === 0) {
    throw new Error("User not found");
  }

  const user = users[0];

  // Verify current password
  if (!user.salt) {
    throw new Error("User has no salt configured");
  }

  const hashedCurrentPassword = await HashPassword(currentPassword, user.salt);

  if (hashedCurrentPassword !== user.password) {
    throw new Error("Current password is incorrect");
  }

  // Generate new salt and hash new password
  const { GenerateSalt } = await import("../utils/crypto.ts");
  const newSalt = GenerateSalt();
  const hashedNewPassword = await HashPassword(newPassword, newSalt);

  // Update password
  await execute(
    `UPDATE "User"
     SET "Password" = $1, "Salt" = $2, "UpdatedDate" = NOW(), "UpdatedBy" = $3
     WHERE "Id" = $4`,
    [hashedNewPassword, newSalt, userId, userId]
  );

  // Invalidate all active login sessions
  await execute(
    `UPDATE "UserLogin"
     SET "Active" = false, "UpdatedDate" = NOW()
     WHERE "UserId" = $1 AND "Active" = true`,
    [userId]
  );

  return true;
}

/**
 * Get user by ID
 * @param userId - The user ID
 * @returns User object or null
 */
export async function GetUserById(userId: string): Promise<(User & { tenant_code?: string }) | null> {
  const users = await query(
    `${USER_SELECT}
     FROM "User" u
     LEFT JOIN "Partner" p ON u."CompanyId" = p."Id"
     WHERE u."Id" = $1`,
    [userId]
  );

  if (!users || users.length === 0) {
    return null;
  }

  return users[0] as User & { tenant_code?: string };
}
