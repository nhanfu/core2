/**
 * Auth Controller
 * Handles authentication API endpoints
 * Migration from CoreAPI AuthController
 */

import { SignIn, refreshToken, SignOut } from "../services/authService.ts";
import type { Token } from "../types/interfaces.ts";

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

    if (!userName) {
      return {
        success: false,
        message: "Username is required",
        statusCode: 400,
      };
    }

    if (!password) {
      return {
        success: false,
        message: "Password is required",
        statusCode: 400,
      };
    }

    // Attempt to sign in
    const token = await SignIn(userName, password);

    return {
      success: true,
      data: token,
      message: "Login successful",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Login error:", error);

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
    const { refreshToken: refreshToken } = body;

    if (!refreshToken) {
      return {
        success: false,
        message: "Refresh token is required",
        statusCode: 400,
      };
    }

    // Attempt to refresh token
    const token = await refreshToken(refreshToken);

    return {
      success: true,
      data: token,
      message: "Token refreshed successfully",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Refresh token error:", error);

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

    if (!refreshToken) {
      return {
        success: false,
        message: "Refresh token is required",
        statusCode: 400,
      };
    }

    // Attempt to sign out
    await SignOut(refreshToken);

    return {
      success: true,
      message: "Logout successful",
      statusCode: 200,
    };
  } catch (error) {
    console.error("Logout error:", error);

    const message = error instanceof Error ? error.message : "Logout failed";

    return {
      success: false,
      message: message,
      statusCode: 500,
    };
  }
}
