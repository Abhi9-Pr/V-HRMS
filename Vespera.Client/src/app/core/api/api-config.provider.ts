import { Provider } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  API_BASE_URL,
  AuthClient,
  DepartmentsClient,
  EmployeesClient,
  HolidaysClient,
  LeaveClient,
  LeaveTypesClient,
  ProxyDelegationsClient,
  TenantsClient,
} from './generated/api-client';

/**
 * NSwag's Angular template emits each `*Client` with a bare `@Injectable()` — no `providedIn`
 * — so unlike our own services they don't register themselves; every generated client used
 * anywhere in the app must be listed here once. Forgetting one only fails at runtime (a
 * NullInjectorError), not at build time, which is why every client gets added here the moment
 * a facade starts using it, not on first-use-discovers-it-'s-broken.
 */
export function provideVesperaApiClients(): Provider[] {
  return [
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
    AuthClient,
    DepartmentsClient,
    TenantsClient,
    EmployeesClient,
    HolidaysClient,
    LeaveClient,
    LeaveTypesClient,
    ProxyDelegationsClient,
  ];
}
