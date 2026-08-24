// Mirrors Vespera.Application.Features.Auth.LoginCommand / LoginResult exactly.
export interface LoginRequest {
  email: string;
  password: string;
  deviceId: string;
  totpCode?: string | null;
}

export interface LoginResult {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

export interface StoredSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  tenantId: string;
}
