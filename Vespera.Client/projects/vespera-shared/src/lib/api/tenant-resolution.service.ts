import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { TenantLookupDto, TenantsClient } from './generated/api-client';

const TENANT_ID_KEY = 'vespera.tenantId';
const TENANT_CODE_KEY = 'vespera.tenantCode';
const TENANT_NAME_KEY = 'vespera.tenantName';

/** Resolves a human-readable tenant code (e.g. "DEMO") to the GUID the pre-auth `X-Tenant-Id`
 * header needs — see GetTenantIdByCodeQuery on the API side. Caches the result in sessionStorage
 * so a page reload during login doesn't need a second round trip. */
@Injectable({ providedIn: 'root' })
export class TenantResolutionService {
  private readonly tenantsClient = inject(TenantsClient);

  resolveByCode(code: string): Observable<TenantLookupDto> {
    return this.tenantsClient.tenants_ByCode(code).pipe(
      tap((lookup) => {
        if (lookup.tenantId) {
          sessionStorage.setItem(TENANT_ID_KEY, lookup.tenantId);
          sessionStorage.setItem(TENANT_CODE_KEY, code);
          sessionStorage.setItem(TENANT_NAME_KEY, lookup.name ?? code);
        }
      }),
    );
  }

  getCachedTenantId(): string | null {
    return sessionStorage.getItem(TENANT_ID_KEY);
  }

  getCachedTenantCode(): string | null {
    return sessionStorage.getItem(TENANT_CODE_KEY);
  }

  getCachedTenantName(): string | null {
    return sessionStorage.getItem(TENANT_NAME_KEY);
  }

  clear(): void {
    sessionStorage.removeItem(TENANT_ID_KEY);
    sessionStorage.removeItem(TENANT_CODE_KEY);
    sessionStorage.removeItem(TENANT_NAME_KEY);
  }
}
