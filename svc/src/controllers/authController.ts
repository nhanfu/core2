/**
 * Auth Controller
 * Handles authentication API endpoints
 * Migration from CoreAPI AuthController
 */

import { signIn, refreshToken as refreshTokenService, signOut } from "../services/authService.ts";
import type { Token } from "../types/interfaces.ts";

const AUTH_DEBUG = (Deno.env.get("AUTH_DEBUG") || "true").toLowerCase() !== "false";

function authDebug(step: string, details: Record<string, unknown> = {}): void {
  if (!AUTH_DEBUG) {
    return;
  }

  console.log(`[AUTH][CONTROLLER] ${step}`, details);
}

// ============================================
// Request/Response Types
// ============================================

/** Request body for login endpoint */
export interface LoginRequest {
  userName: string;
  password: string;
}

/** Request body for refresh token endpoint */
export interface refreshTokenRequest {
  refreshToken: string;
}

/** Request body for logout endpoint */
export interface LogoutRequest {
  refreshToken: string;
}

/** Standard API response */
export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message?: string;
  statusCode: number;
}

// ============================================
// Auth Controllers
// ============================================

/**
 * POST /api/auth/login
 * Sign in with credentials
 * @param userName - The username
 * @param password - The password
 * @returns Token object with accessToken, refreshToken, user data, roles
 */
export async function login(
  body: LoginRequest
): Promise<ApiResponse<Token>> {
  try {
    const { userName, password } = body;
    authDebug("login:request", {
      userName,
      hasPassword: !!password,
    });

    if (!userName) {
      authDebug("login:validation-failed", { reason: "missing-username" });
      return {
        success: false,
        message: "Username is required",
        statusCode: 400,
      };
    }

    if (!password) {
      authDebug("login:validation-failed", { reason: "missing-password", userName });
      return {
        success: false,
        message: "Password is required",
        statusCode: 400,
      };
    }

    // Attempt to sign in
    const token = await signIn(userName, password);
    authDebug("login:success", {
      userId: token.userId,
      userName: token.userName,
      tenantCode: token.tenantCode,
    });

    return {
      success: true,
      data: token,
      message: "Login successful",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Login error:", error);
    authDebug("login:failed", {
      error: error instanceof Error ? error.message : "Login failed",
    });

    const message = error instanceof Error ? error.message : "Login failed";

    return {
      success: false,
      message: message,
      statusCode: 401,
    };
  }
}

/**
 * POST /api/auth/refreshToken
 * Refresh access token using refresh token
 * @param refreshToken - The refresh token
 * @returns New Token object with updated tokens
 */
export async function refreshToken(
  body: refreshTokenRequest
): Promise<ApiResponse<Token>> {
  try {
    const { refreshToken} = body;
    authDebug("refresh:request", { hasRefreshToken: !!refreshToken });

    if (!refreshToken) {
      authDebug("refresh:validation-failed", { reason: "missing-refresh-token" });
      return {
        success: false,
        message: "Refresh token is required",
        statusCode: 400,
      };
    }

    // Attempt to refresh token
    const token = await refreshTokenService(refreshToken);
    authDebug("refresh:success", {
      userId: token.userId,
      userName: token.userName,
    });

    return {
      success: true,
      data: token,
      message: "Token refreshed successfully",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Refresh token error:", error);
    authDebug("refresh:failed", {
      error: error instanceof Error ? error.message : "Token refresh failed",
    });

    const message = error instanceof Error ? error.message : "Token refresh failed";

    return {
      success: false,
      message: message,
      statusCode: 401,
    };
  }
}

/**
 * POST /api/auth/logout
 * Sign out by invalidating refresh token
 * @param refreshToken - The refresh token to invalidate
 */
export async function logout(
  body: LogoutRequest
): Promise<ApiResponse<void>> {
  try {
    const { refreshToken } = body;
    authDebug("logout:request", { hasRefreshToken: !!refreshToken });

    if (!refreshToken) {
      authDebug("logout:validation-failed", { reason: "missing-refresh-token" });
      return {
        success: false,
        message: "Refresh token is required",
        statusCode: 400,
      };
    }

    // Attempt to sign out
    await signOut(refreshToken);
    authDebug("logout:success");

    return {
      success: true,
      message: "Logout successful",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Logout error:", error);
    authDebug("logout:failed", {
      error: error instanceof Error ? error.message : "Logout failed",
    });

    const message = error instanceof Error ? error.message : "Logout failed";

    return {
      success: false,
      message: message,
      statusCode: 500,
    };
  }
}

