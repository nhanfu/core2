import type { RuntimeContext } from "../types.js";
import type { Claim } from "../userService/types.js";

/**
 * JWT Token payload structure
 */
export interface JwtPayload {
  iss?: string;
  sub?: string;
  aud?: string | string[];
  exp?: number;
  nbf?: number;
  iat?: number;
  jti?: string;
  [key: string]: unknown;
}

/**
 * Supabase Auth User
 */
export interface SupabaseUser {
  id: string;
  email?: string;
  email_confirmed_at?: string;
  phone?: string;
  app_metadata?: Record<string, unknown>;
  user_metadata?: Record<string, unknown>;
  aud?: string;
  created_at?: string;
}

/**
 * Auth configuration options
 */
export interface AuthConfig {
  supabaseUrl: string;
  supabaseKey: string;
  jwtSecret?: string;
  issuer?: string;
  audience?: string;
}

/**
 * Supabase Auth Adapter
 * Handles JWT token validation, user session management, and role/claim extraction
 */
export class AuthAdapter {
  private config: AuthConfig;
  private supabaseUrl: string;
  private supabaseKey: string;

  constructor(config: AuthConfig) {
    this.config = config;
    this.supabaseUrl = config.supabaseUrl;
    this.supabaseKey = config.supabaseKey;
  }

  /**
   * Validate JWT token and extract claims
   */
  async validateToken(token: string): Promise<JwtPayload | null> {
    try {
      // For client-side tokens, we'll decode without verification
      // In production, verify with Supabase JWT secret
      const payload = this.decodeJwt(token);
      
      if (!payload) {
        return null;
      }

      // Check expiration
      if (payload.exp && payload.exp < Date.now() / 1000) {
        console.warn("Token has expired");
        return null;
      }

      // Validate issuer if configured
      if (this.config.issuer && payload.iss !== this.config.issuer) {
        console.warn("Invalid token issuer");
        return null;
      }

      // Validate audience if configured
      if (this.config.audience) {
        const aud = Array.isArray(payload.aud) ? payload.aud : [payload.aud];
        if (!aud.includes(this.config.audience)) {
          console.warn("Invalid token audience");
          return null;
        }
      }

      return payload;
    } catch (error) {
      console.error("Token validation error:", error);
      return null;
    }
  }

  /**
   * Decode JWT token (without verification)
   */
  decodeJwt(token: string): JwtPayload | null {
    try {
      const parts = token.split(".");
      if (parts.length !== 3) {
        return null;
      }

      const payload = parts[1];
      const decoded = this.base64UrlDecode(payload);
      return JSON.parse(decoded) as JwtPayload;
    } catch (error) {
      console.error("JWT decode error:", error);
      return null;
    }
  }

  /**
   * Extract claims from JWT payload for UserService
   */
  extractClaims(payload: JwtPayload): Claim[] {
    const claims: Claim[] = [];

    // Standard claims
    if (payload.sub) {
      claims.push({ type: "UserId", value: payload.sub });
    }

    if (payload.email) {
      claims.push({ type: "Email", value: payload.email as string });
    }

    // Custom claims from Supabase
    if (payload.app_metadata) {
      const appMeta = payload.app_metadata as Record<string, unknown>;
      
      if (appMeta.roles) {
        const roles = Array.isArray(appMeta.roles) ? appMeta.roles : [appMeta.roles];
        roles.forEach((role) => {
          claims.push({ type: "Role", value: role as string });
        });
      }

      if (appMeta.role_ids) {
        const roleIds = Array.isArray(appMeta.role_ids) ? appMeta.role_ids : [appMeta.role_ids];
        roleIds.forEach((roleId) => {
          claims.push({ type: "RoleIds", value: roleId as string });
        });
      }
    }

    // User metadata
    if (payload.user_metadata) {
      const userMeta = payload.user_metadata as Record<string, unknown>;
      
      if (userMeta.full_name) {
        claims.push({ type: "FullName", value: userMeta.full_name as string });
      }

      if (userMeta.avatar_url) {
        claims.push({ type: "Avatar", value: userMeta.avatar_url as string });
      }
    }

    // Tenant/team info
    if (payload.tenant_id) {
      claims.push({ type: "TenantCode", value: payload.tenant_id as string });
    }

    if (payload.team_id) {
      claims.push({ type: "TeamId", value: payload.team_id as string });
    }

    return claims;
  }

  /**
   * Create RuntimeContext from claims
   */
  createRuntimeContext(claims: Claim[]): RuntimeContext {
    const context: RuntimeContext = {
      roleIds: [],
      roleNames: [],
      variables: {},
    };

    claims.forEach((claim) => {
      switch (claim.type) {
        case "TenantCode":
        case "TenantId":
          context.tenant = claim.value;
          break;
        case "UserId":
          context.userId = claim.value;
          break;
        case "RoleIds":
          context.roleIds?.push(claim.value);
          break;
        case "Role":
        case "Roles":
          context.roleNames?.push(claim.value);
          break;
        default:
          if (!context.variables) {
            context.variables = {};
          }
          context.variables[claim.type] = claim.value;
      }
    });

    return context;
  }

  /**
   * Get user info from Supabase
   */
  async getUser(token: string): Promise<SupabaseUser | null> {
    try {
      const response = await fetch(`${this.supabaseUrl}/auth/v1/user`, {
        headers: {
          Authorization: `Bearer ${token}`,
          Apikey: this.supabaseKey,
        },
      });

      if (!response.ok) {
        console.error("Failed to get user:", response.statusText);
        return null;
      }

      return (await response.json()) as SupabaseUser;
    } catch (error) {
      console.error("Get user error:", error);
      return null;
    }
  }

  /**
   * Sign in with email/password
   */
  async signIn(email: string, password: string): Promise<{ session: { access_token: string; refresh_token: string } | null; user: SupabaseUser | null; error: string | null }> {
    try {
      const response = await fetch(`${this.supabaseUrl}/auth/v1/token?grant_type=password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Apikey: this.supabaseKey,
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const error = await response.json();
        return { session: null, user: null, error: error.error_description || error.msg || "Sign in failed" };
      }

      const data = await response.json();
      return {
        session: { access_token: data.access_token, refresh_token: data.refresh_token },
        user: data.user as SupabaseUser,
        error: null,
      };
    } catch (error) {
      console.error("Sign in error:", error);
      return { session: null, user: null, error: "Network error" };
    }
  }

  /**
   * Sign out
   */
  async signOut(token: string): Promise<boolean> {
    try {
      const response = await fetch(`${this.supabaseUrl}/auth/v1/logout`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          Apikey: this.supabaseKey,
        },
      });

      return response.ok;
    } catch (error) {
      console.error("Sign out error:", error);
      return false;
    }
  }

  /**
   * Refresh session
   */
  async refreshSession(refreshToken: string): Promise<{ session: { access_token: string; refresh_token: string } | null; error: string | null }> {
    try {
      const response = await fetch(`${this.supabaseUrl}/auth/v1/token?grant_type=refresh_token`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Apikey: this.supabaseKey,
        },
        body: JSON.stringify({ refresh_token: refreshToken }),
      });

      if (!response.ok) {
        const error = await response.json();
        return { session: null, error: error.error_description || "Refresh failed" };
      }

      const data = await response.json();
      return {
        session: { access_token: data.access_token, refresh_token: data.refresh_token },
        error: null,
      };
    } catch (error) {
      console.error("Refresh session error:", error);
      return { session: null, error: "Network error" };
    }
  }

  /**
   * Base64 URL decode
   */
  private base64UrlDecode(str: string): string {
    // Replace URL-safe characters
    let base64 = str.replace(/-/g, "+").replace(/_/g, "/");
    
    // Pad with =
    while (base64.length % 4) {
      base64 += "=";
    }

    return atob(base64);
  }
}

/**
 * Create an AuthAdapter instance
 */
export function createAuthAdapter(config: AuthConfig): AuthAdapter {
  return new AuthAdapter(config);
}

/**
 * Parse JWT token without verification (for debugging)
 */
export function parseJwt(token: string): JwtPayload | null {
  const adapter = new AuthAdapter({ supabaseUrl: "", supabaseKey: "" });
  return adapter.decodeJwt(token);
}
