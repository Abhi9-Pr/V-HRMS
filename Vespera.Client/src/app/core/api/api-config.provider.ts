import { Provider } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  API_BASE_URL,
  AssetRecoveriesClient,
  AssetsClient,
  AuthClient,
  CandidatesClient,
  DepartmentsClient,
  ExpensePoliciesClient,
  ExpenseSettlementsClient,
  ExpensesClient,
  InterviewsClient,
  LicensesClient,
  OffboardingChecklistsClient,
  OffersClient,
  PublicHolidaysClient,
  PublicJobsClient,
  RequisitionsClient,
  SlaPoliciesClient,
  TenantsClient,
  TicketCategoriesClient,
  TicketsClient,
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
    // Phase 11
    ExpensesClient,
    ExpensePoliciesClient,
    ExpenseSettlementsClient,
    AssetsClient,
    LicensesClient,
    AssetRecoveriesClient,
    OffboardingChecklistsClient,
    RequisitionsClient,
    CandidatesClient,
    InterviewsClient,
    OffersClient,
    PublicJobsClient,
    TicketsClient,
    TicketCategoriesClient,
    SlaPoliciesClient,
    PublicHolidaysClient,
  ];
}
