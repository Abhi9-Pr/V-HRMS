export interface AuthSession {
  userId: string;
  email: string;
  tenantId: string;
  roles: string[];
  permissions: string[];
  accessToken: string;
  accessTokenExpiresAt: Date;
}
