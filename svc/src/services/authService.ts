/**
 * Authentication Service
 * Handles user authentication, token generation, and refresh
 * Compatible with CoreAPI's AuthService.cs
 */

import { query, execute, insert } from "../database/postgresClient.ts";
import { HashPassword } from "../utils/crypto.ts";
import {
  generateAccessToken,
  generateRefreshToken,
  verifyToken,
  ACCESS_TOKEN_EXPIRY,
  REFRESH_TOKEN_EXPIRY,
} from "../utils/jwt.ts";
import type { User, Token, UserLogin, Partner } from "../types/interfaces.ts";

// Token configuration from environment
const JWT_SECRET = Deno.env.get("JWT_SECRET") || "your-secret-key";
const JWT_ISSUER = Deno.env.get("JWT_ISSUER") || "CoreAPI";
const JWT_AUDIENCE = Deno.env.get("JWT_AUDIENCE") || "CoreAPI";
const AUTH_DEBUG = (Deno.env.get("AUTH_DEBUG") || "true").toLowerCase() !== "false";

function authDebug(step: string, details: Record<string, unknown> = {}): void {
  if (!AUTH_DEBUG) {
    return;
  }

  console.log(`[AUTH][SERVICE] ${step}`, details);
}

function maskToken(token: string | null | undefined): string {
  if (!token) {
    return "";
  }

  if (token.length <= 12) {
    return `${token.slice(0, 4)}...`;
  }

  return `${token.slice(0, 8)}...${token.slice(-4)}`;
}

const USER_SELECT = `
  SELECT
    u.id AS id,
    u.code AS code,
    u.email AS email,
    u.password AS password,
    u.salt AS salt,
    u.company_id AS "companyId",
    u.user_name AS "userName",
    u.full_name AS "fullName",
    u.address AS address,
    u.avatar AS avatar,
    u.ssn AS ssn,
    u.phone_number AS "phoneNumber",
    u.team_id AS "teamId",
    u.partner_id AS "partnerId",
    u.active AS active,
    u.department_id AS "departmentId",
    u.position_id AS "positionId",
    u.role_ids AS "roleIds",
    u.login_failed_count AS "loginFailedCount",
    u.last_failed_login AS "lastFailedLogin",
    u.last_login AS "lastLogin",
    p.code AS tenant_code,
    p.name AS tenant_name
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
      `SELECT role_id AS "roleId"
       FROM user_role
       WHERE user_id = $1 AND active = true`,
      [userId]
    );

    if (roles && roles.length > 0) {
      return roles.map((r: any) => r.roleId);
    }

    // Fallback: use the denormalized RoleIds field on the user row.
    const users = await query(
      `SELECT role_ids AS "roleIds"
       FROM users
       WHERE id = $1`,
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
      `SELECT name AS name
       FROM role
       WHERE id IN (${placeholders}) AND active = true`,
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
         id AS id,
         name AS name,
         code AS code,
         address AS address,
         phone_number AS "phoneNumber",
         mail AS mail,
         email AS email,
         active AS active,
         is_public AS "isPublic"
       FROM partner
       WHERE code = $1 AND active = true`,
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
export async function signIn(
  userName: string,
  password: string
): Promise<Token> {
  authDebug("signin:start", { userName });

  const users = await query(
    `${USER_SELECT}
     FROM users u
     LEFT JOIN partner p ON u.company_id = p.id
     WHERE u.user_name = $1 AND u.active = true`,
    [userName]
  );

  authDebug("signin:user-query-complete", {
    userName,
    userCount: users?.length || 0,
  });

  if (!users || users.length === 0) {
    authDebug("signin:user-not-found", { userName });
    throw new Error("Invalid username or password");
  }

  const user = {
    ...(users[0] as User & { tenant_code?: string }),
    tenant_code: (users[0] as { tenant_code?: string }).tenant_code ||
      Deno.env.get("TENANT_CODE") || "system",
  };

  // Check if user is active
  if (!user.active) {
    authDebug("signin:user-inactive", { userId: user.id, userName });
    throw new Error("User account is inactive");
  }

  // Step 2: Hash provided password with user's salt
  if (!user.salt) {
    authDebug("signin:missing-salt", { userId: user.id, userName });
    throw new Error("User has no salt configured");
  }

  const hashedPassword = await HashPassword(password, user.salt);
  authDebug("signin:password-hashed", { userId: user.id, userName });

  // Step 3: Compare with stored password
  if (hashedPassword !== user.password) {
    authDebug("signin:password-mismatch", { userId: user.id, userName });
    // Increment failed login count
    await execute(
      `UPDATE users
       SET login_failed_count = COALESCE(login_failed_count, 0) + 1,
           last_failed_login = NOW()
       WHERE id = $1`,
      [user.id]
    );
    authDebug("signin:failed-login-count-incremented", { userId: user.id });
    throw new Error("Invalid username or password");
  }

  // Step 4: Generate access and refresh tokens
  authDebug("signin:password-verified", { userId: user.id, userName });
  const accessToken = await GenerateAccessToken(user);
  const refreshToken = generateRefreshTokenInternal();

  // Calculate expiration dates
  const accessTokenExp = getExpirationDate(ACCESS_TOKEN_EXPIRY);
  const refreshTokenExp = getExpirationDate(REFRESH_TOKEN_EXPIRY);

  // Get additional user data
  const roleIds = await getUserRoleIds(user.id);
  const roleNames = await getRoleNames(roleIds);
  const centerIds = await getUserCenterIds(user.id);
  authDebug("signin:user-metadata-loaded", {
    userId: user.id,
    roleCount: roleIds.length,
    roleNames,
    centerCount: centerIds.length,
  });

  // Get tenant/company info
  const tenant = await getTenant(user.tenant_code || "");
  authDebug("signin:tenant-loaded", {
    userId: user.id,
    tenantCode: user.tenant_code || "",
    tenantFound: !!tenant,
  });

  // Step 5: Store refresh token in database
  try {
    await insert("user_login", {
      "id": crypto.randomUUID(),
      "user_id": user.id,
      "access_token": accessToken,
      "refresh_token": refreshToken,
      "access_token_exp": accessTokenExp.toISOString(),
      "refresh_token_exp": refreshTokenExp.toISOString(),
      "active": true,
      "inserted_date": new Date().toISOString(),
      "inserted_by": user.id,
    });

    // Update last login
    await execute(
      `UPDATE users
       SET last_login = NOW(), login_failed_count = 0
       WHERE id = $1`,
      [user.id]
    );
    authDebug("signin:login-record-stored", {
      userId: user.id,
      accessToken: maskToken(accessToken),
      refreshToken: maskToken(refreshToken),
      accessTokenExp: accessTokenExp.toISOString(),
      refreshTokenExp: refreshTokenExp.toISOString(),
    });
  } catch (error) {
    console.error("Error storing login record:", error);
    authDebug("signin:login-record-store-failed", {
      userId: user.id,
      error: error instanceof Error ? error.message : String(error),
    });
    // Continue even if login record fails
  }

  // Step 6: Return Token object
  authDebug("signin:success", {
    userId: user.id,
    userName,
    tenantCode: user.tenant_code || "system",
  });
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
export async function refreshToken(refreshTokenValue: string): Promise<Token> {
  authDebug("refresh:start", { refreshToken: maskToken(refreshTokenValue) });
  // Step 1: Find the login record with this refresh token
  const loginRecords = await query(
    `SELECT
       ul.user_id AS "userId",
       ul.active AS active,
       ${USER_SELECT.replace(/^(\s*SELECT\s*)/m, "")}
     FROM user_login ul
     JOIN users u ON ul.user_id = u.id
     LEFT JOIN partner p ON u.company_id = p.id
     WHERE ul.refresh_token = $1 AND ul.active = true
       AND ul.refresh_token_exp > NOW()`,
    [refreshTokenValue]
  );

  authDebug("refresh:lookup-complete", {
    refreshToken: maskToken(refreshTokenValue),
    recordCount: loginRecords?.length || 0,
  });

  if (!loginRecords || loginRecords.length === 0) {
    authDebug("refresh:token-not-found", { refreshToken: maskToken(refreshTokenValue) });
    throw new Error("Invalid or expired refresh token");
  }

  const loginRecord = loginRecords[0];

  // Step 2: Check if user is still active
  if (!loginRecord.active) {
    authDebug("refresh:user-inactive", { userId: loginRecord.userId });
    throw new Error("User account is inactive");
  }

  // Step 3: Get fresh user data
  const users = await query(
    `${USER_SELECT}
     FROM users u
     LEFT JOIN partner p ON u.company_id = p.id
     WHERE u.id = $1 AND u.active = true`,
    [loginRecord.userId]
  );

  if (!users || users.length === 0) {
    authDebug("refresh:user-not-found", { userId: loginRecord.userId });
    throw new Error("User not found or inactive");
  }

  const user = users[0] as User & { tenant_code?: string };

  // Step 4: Generate new access token
  const newAccessToken = await GenerateAccessToken(user);
  const newRefreshToken = generateRefreshTokenInternal();

  // Calculate new expiration dates
  const newAccessTokenExp = getExpirationDate(ACCESS_TOKEN_EXPIRY);
  const newRefreshTokenExp = getExpirationDate(REFRESH_TOKEN_EXPIRY);

  // Get role and center info
  const roleIds = await getUserRoleIds(user.id);
  const roleNames = await getRoleNames(roleIds);
  const centerIds = await getUserCenterIds(user.id);
  authDebug("refresh:user-metadata-loaded", {
    userId: user.id,
    roleCount: roleIds.length,
    centerCount: centerIds.length,
  });

  // Get tenant/company info
  const tenant = user.tenant_code ? await getTenant(user.tenant_code) : null;

  // Step 5: Deactivate old login record and create new one
  try {
    await execute(
      `UPDATE user_login
       SET active = false, updated_date = NOW()
       WHERE refresh_token = $1`,
      [refreshTokenValue]
    );

    await insert("user_login", {
      "id": crypto.randomUUID(),
      "user_id": user.id,
      "access_token": newAccessToken,
      "refresh_token": newRefreshToken,
      "access_token_exp": newAccessTokenExp.toISOString(),
      "refresh_token_exp": newRefreshTokenExp.toISOString(),
      "active": true,
      "inserted_date": new Date().toISOString(),
      "inserted_by": user.id,
    });
    authDebug("refresh:login-record-rotated", {
      userId: user.id,
      oldRefreshToken: maskToken(refreshTokenValue),
      newRefreshToken: maskToken(newRefreshToken),
      accessToken: maskToken(newAccessToken),
    });
  } catch (error) {
    console.error("Error updating login record:", error);
    authDebug("refresh:login-record-rotate-failed", {
      userId: user.id,
      error: error instanceof Error ? error.message : String(error),
    });
    // Continue even if update fails
  }

  // Step 6: Return new Token object
  authDebug("refresh:success", {
    userId: user.id,
    tenantCode: user.tenant_code || "",
  });
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
  authDebug("access-token:generate", {
    userId: user.id,
    userName: user.userName,
    roleCount: roleIds.length,
    tenantCode: (user as any).tenant_code || user.companyId,
  });

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
export function generateRefreshTokenInternal(): string {
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
    authDebug("access-token:validate-start", { token: maskToken(token) });
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

    authDebug("access-token:validate-success", {
      token: maskToken(token),
      userId: payload.userId || "",
    });
    return payload;
  } catch (error) {
    authDebug("access-token:validate-failed", {
      token: maskToken(token),
      error: error instanceof Error ? error.message : String(error),
    });
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
export async function signOut(refreshToken: string): Promise<void> {
  try {
    authDebug("signout:start", { refreshToken: maskToken(refreshToken) });
    await execute(
      `UPDATE user_login
       SET active = false, updated_date = NOW()
       WHERE refresh_token = $1`,
      [refreshToken]
    );
    authDebug("signout:success", { refreshToken: maskToken(refreshToken) });
  } catch (error) {
    console.error("Error signing out:", error);
    authDebug("signout:failed", {
      refreshToken: maskToken(refreshToken),
      error: error instanceof Error ? error.message : String(error),
    });
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
     FROM users u
     LEFT JOIN partner p ON u.company_id = p.id
     WHERE u.id = $1 AND u.active = true`,
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
    `UPDATE users
     SET password = $1, salt = $2, updated_date = NOW(), updated_by = $3
     WHERE id = $4`,
    [hashedNewPassword, newSalt, userId, userId]
  );

  // Invalidate all active login sessions
  await execute(
    `UPDATE user_login
     SET active = false, updated_date = NOW()
     WHERE user_id = $1 AND active = true`,
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
     FROM users u
     LEFT JOIN partner p ON u.company_id = p.id
     WHERE u.id = $1`,
    [userId]
  );

  if (!users || users.length === 0) {
    return null;
  }

  return users[0] as User & { tenant_code?: string };
}

