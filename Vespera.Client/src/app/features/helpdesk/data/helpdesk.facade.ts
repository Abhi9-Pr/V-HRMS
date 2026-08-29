import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { DataTableQuery } from '../../../shared/data-table/data-table.model';
import {
  AddCommentRequest,
  ApiError,
  AssignTicketRequest,
  CreatePublicHolidayRequest,
  CreatePublicHolidayResponse,
  CreateSlaPolicyRequest,
  CreateSlaPolicyResponse,
  CreateTicketCategoryRequest,
  CreateTicketCategoryResponse,
  PublicHolidayDto,
  PublicHolidaysClient,
  RaiseTicketRequest,
  RaiseTicketResponse,
  RateSatisfactionRequest,
  SlaComplianceReportDto,
  SlaPoliciesClient,
  SlaPolicyDto,
  TicketCategoriesClient,
  TicketCategoryDto,
  TicketDto,
  TicketSummaryDto,
  TicketsClient,
  UploadAttachmentResponse,
} from 'vespera-shared';

/**
 * Wraps TicketsClient/TicketCategoriesClient/SlaPoliciesClient/PublicHolidaysClient behind
 * signals + plain methods, same split as ExpensesFacade/AssetsFacade/RecruitmentFacade (see
 * docs/CONTRIBUTING-frontend.md / docs/frontend-state.md): `load*` methods manage their own
 * loading/error signals and subscribe internally, mutating methods delegate straight to the
 * generated client and return the Observable for the calling component to handle.
 */
@Injectable({ providedIn: 'root' })
export class HelpdeskFacade {
  private readonly ticketsClient = inject(TicketsClient);
  private readonly categoriesClient = inject(TicketCategoriesClient);
  private readonly policiesClient = inject(SlaPoliciesClient);
  private readonly holidaysClient = inject(PublicHolidaysClient);

  private readonly ticketsSignal = signal<TicketSummaryDto[]>([]);
  private readonly ticketsTotalCountSignal = signal(0);
  private readonly ticketsLoadingSignal = signal(false);
  private readonly ticketsErrorSignal = signal<ApiError | null>(null);

  private readonly ticketSignal = signal<TicketDto | null>(null);
  private readonly ticketLoadingSignal = signal(false);
  private readonly ticketErrorSignal = signal<ApiError | null>(null);

  private readonly slaReportSignal = signal<SlaComplianceReportDto | null>(null);
  private readonly slaReportLoadingSignal = signal(false);
  private readonly slaReportErrorSignal = signal<ApiError | null>(null);

  private readonly categoriesSignal = signal<TicketCategoryDto[]>([]);
  private readonly categoriesTotalCountSignal = signal(0);
  private readonly categoriesLoadingSignal = signal(false);
  private readonly categoriesErrorSignal = signal<ApiError | null>(null);

  private readonly policiesSignal = signal<SlaPolicyDto[]>([]);
  private readonly policiesTotalCountSignal = signal(0);
  private readonly policiesLoadingSignal = signal(false);
  private readonly policiesErrorSignal = signal<ApiError | null>(null);

  private readonly holidaysSignal = signal<PublicHolidayDto[]>([]);
  private readonly holidaysTotalCountSignal = signal(0);
  private readonly holidaysLoadingSignal = signal(false);
  private readonly holidaysErrorSignal = signal<ApiError | null>(null);

  readonly tickets = this.ticketsSignal.asReadonly();
  readonly ticketsTotalCount = this.ticketsTotalCountSignal.asReadonly();
  readonly ticketsLoading = this.ticketsLoadingSignal.asReadonly();
  readonly ticketsError = this.ticketsErrorSignal.asReadonly();

  readonly ticket = this.ticketSignal.asReadonly();
  readonly ticketLoading = this.ticketLoadingSignal.asReadonly();
  readonly ticketError = this.ticketErrorSignal.asReadonly();

  readonly slaReport = this.slaReportSignal.asReadonly();
  readonly slaReportLoading = this.slaReportLoadingSignal.asReadonly();
  readonly slaReportError = this.slaReportErrorSignal.asReadonly();

  readonly categories = this.categoriesSignal.asReadonly();
  readonly categoriesTotalCount = this.categoriesTotalCountSignal.asReadonly();
  readonly categoriesLoading = this.categoriesLoadingSignal.asReadonly();
  readonly categoriesError = this.categoriesErrorSignal.asReadonly();

  readonly policies = this.policiesSignal.asReadonly();
  readonly policiesTotalCount = this.policiesTotalCountSignal.asReadonly();
  readonly policiesLoading = this.policiesLoadingSignal.asReadonly();
  readonly policiesError = this.policiesErrorSignal.asReadonly();

  readonly holidays = this.holidaysSignal.asReadonly();
  readonly holidaysTotalCount = this.holidaysTotalCountSignal.asReadonly();
  readonly holidaysLoading = this.holidaysLoadingSignal.asReadonly();
  readonly holidaysError = this.holidaysErrorSignal.asReadonly();

  loadTickets(query: DataTableQuery): void {
    this.ticketsLoadingSignal.set(true);
    this.ticketsErrorSignal.set(null);

    this.ticketsClient.tickets_GetTickets(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.ticketsSignal.set(result.items ?? []);
        this.ticketsTotalCountSignal.set(result.totalCount ?? 0);
        this.ticketsLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.ticketsErrorSignal.set(apiError);
        this.ticketsLoadingSignal.set(false);
      },
    });
  }

  loadTicketById(id: string): void {
    this.ticketLoadingSignal.set(true);
    this.ticketErrorSignal.set(null);

    this.ticketsClient.tickets_GetTicketById(id).subscribe({
      next: (ticket) => {
        this.ticketSignal.set(ticket);
        this.ticketLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.ticketErrorSignal.set(apiError);
        this.ticketLoadingSignal.set(false);
      },
    });
  }

  loadSlaComplianceReport(): void {
    this.slaReportLoadingSignal.set(true);
    this.slaReportErrorSignal.set(null);

    this.ticketsClient.tickets_GetSlaComplianceReport().subscribe({
      next: (report) => {
        this.slaReportSignal.set(report);
        this.slaReportLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.slaReportErrorSignal.set(apiError);
        this.slaReportLoadingSignal.set(false);
      },
    });
  }

  loadCategories(query: DataTableQuery): void {
    this.categoriesLoadingSignal.set(true);
    this.categoriesErrorSignal.set(null);

    this.categoriesClient
      .ticketCategories_GetCategories(query.page, query.pageSize, query.sortBy, query.sortDescending)
      .subscribe({
        next: (result) => {
          this.categoriesSignal.set(result.items ?? []);
          this.categoriesTotalCountSignal.set(result.totalCount ?? 0);
          this.categoriesLoadingSignal.set(false);
        },
        error: (apiError: ApiError) => {
          this.categoriesErrorSignal.set(apiError);
          this.categoriesLoadingSignal.set(false);
        },
      });
  }

  loadPolicies(query: DataTableQuery): void {
    this.policiesLoadingSignal.set(true);
    this.policiesErrorSignal.set(null);

    this.policiesClient
      .slaPolicies_GetPolicies(query.page, query.pageSize, query.sortBy, query.sortDescending)
      .subscribe({
        next: (result) => {
          this.policiesSignal.set(result.items ?? []);
          this.policiesTotalCountSignal.set(result.totalCount ?? 0);
          this.policiesLoadingSignal.set(false);
        },
        error: (apiError: ApiError) => {
          this.policiesErrorSignal.set(apiError);
          this.policiesLoadingSignal.set(false);
        },
      });
  }

  loadHolidays(query: DataTableQuery): void {
    this.holidaysLoadingSignal.set(true);
    this.holidaysErrorSignal.set(null);

    this.holidaysClient
      .publicHolidays_GetHolidays(query.page, query.pageSize, query.sortBy, query.sortDescending)
      .subscribe({
        next: (result) => {
          this.holidaysSignal.set(result.items ?? []);
          this.holidaysTotalCountSignal.set(result.totalCount ?? 0);
          this.holidaysLoadingSignal.set(false);
        },
        error: (apiError: ApiError) => {
          this.holidaysErrorSignal.set(apiError);
          this.holidaysLoadingSignal.set(false);
        },
      });
  }

  raiseTicket(request: RaiseTicketRequest): Observable<RaiseTicketResponse> {
    return this.ticketsClient.tickets_RaiseTicket(crypto.randomUUID(), request);
  }

  uploadAttachment(ticketId: string, file: File): Observable<UploadAttachmentResponse> {
    return this.ticketsClient.tickets_UploadAttachment(ticketId, crypto.randomUUID(), {
      data: file,
      fileName: file.name,
    });
  }

  addComment(ticketId: string, request: AddCommentRequest): Observable<void> {
    return this.ticketsClient.tickets_AddComment(ticketId, crypto.randomUUID(), request);
  }

  assignTicket(ticketId: string, request: AssignTicketRequest): Observable<void> {
    return this.ticketsClient.tickets_Assign(ticketId, crypto.randomUUID(), request);
  }

  resolveTicket(ticketId: string): Observable<void> {
    return this.ticketsClient.tickets_Resolve(ticketId, crypto.randomUUID());
  }

  closeTicket(ticketId: string): Observable<void> {
    return this.ticketsClient.tickets_Close(ticketId, crypto.randomUUID());
  }

  rateSatisfaction(ticketId: string, request: RateSatisfactionRequest): Observable<void> {
    return this.ticketsClient.tickets_RateSatisfaction(ticketId, crypto.randomUUID(), request);
  }

  createCategory(request: CreateTicketCategoryRequest): Observable<CreateTicketCategoryResponse> {
    return this.categoriesClient.ticketCategories_Create(crypto.randomUUID(), request);
  }

  createPolicy(request: CreateSlaPolicyRequest): Observable<CreateSlaPolicyResponse> {
    return this.policiesClient.slaPolicies_Create(crypto.randomUUID(), request);
  }

  createHoliday(request: CreatePublicHolidayRequest): Observable<CreatePublicHolidayResponse> {
    return this.holidaysClient.publicHolidays_Create(crypto.randomUUID(), request);
  }
}
