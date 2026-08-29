import { Provider } from '@angular/core';
import {
  AnnouncementsClient,
  API_BASE_URL,
  AssetRecoveriesClient,
  AssetsClient,
  AttendanceClient,
  AuthClient,
  CandidatesClient,
  CorporateEventsClient,
  DashboardClient,
  DepartmentsClient,
  ExpensePoliciesClient,
  ExpenseSettlementsClient,
  ExpensesClient,
  InterviewsClient,
  LicensesClient,
  LocationsClient,
  OffboardingChecklistsClient,
  OffersClient,
  PublicHolidaysClient,
  PublicJobsClient,
  RequisitionsClient,
  SlaPoliciesClient,
  TenantsClient,
  TicketCategoriesClient,
  TicketsClient,
  TodosClient,
} from './generated/api-client';

/**
 * NSwag's Angular template emits each `*Client` with a bare `@Injectable()` — no `providedIn`
 * — so unlike our own services they don't register themselves; every generated client used
 * anywhere in the app must be listed here once. Forgetting one only fails at runtime (a
 * NullInjectorError), not at build time, which is why every client gets added here the moment
 * a facade starts using it, not on first-use-discovers-it-'s-broken.
 */
export function provideVesperaApiClients(apiBaseUrl: string): Provider[] {
  return [
    { provide: API_BASE_URL, useValue: apiBaseUrl },
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
    // Landing dashboard
    AttendanceClient,
    DashboardClient,
    TodosClient,
    AnnouncementsClient,
    CorporateEventsClient,
    LocationsClient,
  ];
}
