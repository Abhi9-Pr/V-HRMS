import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { DataTableQuery } from '../../../shared/data-table/data-table.model';
import {
  AddLineRequest,
  ApiError,
  CreateExpensePolicyRequest,
  CreateExpensePolicyResponse,
  DecideApprovalRequest,
  ExpenseClaimDto,
  ExpensePoliciesClient,
  ExpensePolicyDto,
  ExpenseSettlementsClient,
  ExpensesClient,
  OpenClaimRequest,
  OpenExpenseClaimResponse,
  SettleExpenseClaimRequest,
  SubmitExpenseClaimResultDto,
  UploadExpenseReceiptResultDto,
} from 'vespera-shared';

/**
 * Wraps ExpensesClient/ExpensePoliciesClient/ExpenseSettlementsClient behind signals + plain
 * methods, same shape as DepartmentsFacade (see docs/CONTRIBUTING-frontend.md /
 * docs/frontend-state.md): `load*` methods manage their own loading/error signals, mutating
 * methods delegate straight to the generated client and let the caller (a component) handle
 * success/error — see department-form-dialog.component.ts for that half of the pattern.
 */
@Injectable({ providedIn: 'root' })
export class ExpensesFacade {
  private readonly expensesClient = inject(ExpensesClient);
  private readonly policiesClient = inject(ExpensePoliciesClient);
  private readonly settlementsClient = inject(ExpenseSettlementsClient);

  private readonly claimsSignal = signal<ExpenseClaimDto[]>([]);
  private readonly claimsTotalCountSignal = signal(0);
  private readonly claimsLoadingSignal = signal(false);
  private readonly claimsErrorSignal = signal<ApiError | null>(null);

  private readonly claimSignal = signal<ExpenseClaimDto | null>(null);
  private readonly claimLoadingSignal = signal(false);
  private readonly claimErrorSignal = signal<ApiError | null>(null);

  private readonly pendingApprovalsSignal = signal<ExpenseClaimDto[]>([]);
  private readonly pendingApprovalsTotalCountSignal = signal(0);
  private readonly pendingApprovalsLoadingSignal = signal(false);
  private readonly pendingApprovalsErrorSignal = signal<ApiError | null>(null);

  private readonly policiesSignal = signal<ExpensePolicyDto[]>([]);
  private readonly policiesTotalCountSignal = signal(0);
  private readonly policiesLoadingSignal = signal(false);
  private readonly policiesErrorSignal = signal<ApiError | null>(null);

  readonly claims = this.claimsSignal.asReadonly();
  readonly claimsTotalCount = this.claimsTotalCountSignal.asReadonly();
  readonly claimsLoading = this.claimsLoadingSignal.asReadonly();
  readonly claimsError = this.claimsErrorSignal.asReadonly();

  readonly claim = this.claimSignal.asReadonly();
  readonly claimLoading = this.claimLoadingSignal.asReadonly();
  readonly claimError = this.claimErrorSignal.asReadonly();

  readonly pendingApprovals = this.pendingApprovalsSignal.asReadonly();
  readonly pendingApprovalsTotalCount = this.pendingApprovalsTotalCountSignal.asReadonly();
  readonly pendingApprovalsLoading = this.pendingApprovalsLoadingSignal.asReadonly();
  readonly pendingApprovalsError = this.pendingApprovalsErrorSignal.asReadonly();

  readonly policies = this.policiesSignal.asReadonly();
  readonly policiesTotalCount = this.policiesTotalCountSignal.asReadonly();
  readonly policiesLoading = this.policiesLoadingSignal.asReadonly();
  readonly policiesError = this.policiesErrorSignal.asReadonly();

  loadMyClaims(query: DataTableQuery): void {
    this.claimsLoadingSignal.set(true);
    this.claimsErrorSignal.set(null);

    this.expensesClient.expenses_GetMyClaims(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.claimsSignal.set(result.items ?? []);
        this.claimsTotalCountSignal.set(result.totalCount ?? 0);
        this.claimsLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.claimsErrorSignal.set(apiError);
        this.claimsLoadingSignal.set(false);
      },
    });
  }

  loadClaimById(id: string): void {
    this.claimLoadingSignal.set(true);
    this.claimErrorSignal.set(null);

    this.expensesClient.expenses_GetClaimById(id).subscribe({
      next: (claim) => {
        this.claimSignal.set(claim);
        this.claimLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.claimErrorSignal.set(apiError);
        this.claimLoadingSignal.set(false);
      },
    });
  }

  loadPendingApprovals(query: DataTableQuery): void {
    this.pendingApprovalsLoadingSignal.set(true);
    this.pendingApprovalsErrorSignal.set(null);

    this.expensesClient
      .expenses_GetPendingApprovals(query.page, query.pageSize, query.sortBy, query.sortDescending)
      .subscribe({
        next: (result) => {
          this.pendingApprovalsSignal.set(result.items ?? []);
          this.pendingApprovalsTotalCountSignal.set(result.totalCount ?? 0);
          this.pendingApprovalsLoadingSignal.set(false);
        },
        error: (apiError: ApiError) => {
          this.pendingApprovalsErrorSignal.set(apiError);
          this.pendingApprovalsLoadingSignal.set(false);
        },
      });
  }

  loadPolicies(query: DataTableQuery): void {
    this.policiesLoadingSignal.set(true);
    this.policiesErrorSignal.set(null);

    this.policiesClient.expensePolicies_Get(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
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

  openClaim(request: OpenClaimRequest): Observable<OpenExpenseClaimResponse> {
    return this.expensesClient.expenses_OpenClaim(crypto.randomUUID(), request);
  }

  uploadReceipt(claimId: string, file: File): Observable<UploadExpenseReceiptResultDto> {
    return this.expensesClient.expenses_UploadReceipt(claimId, crypto.randomUUID(), {
      data: file,
      fileName: file.name,
    });
  }

  addLine(claimId: string, request: AddLineRequest): Observable<void> {
    return this.expensesClient.expenses_AddLine(claimId, crypto.randomUUID(), request);
  }

  submitClaim(claimId: string): Observable<SubmitExpenseClaimResultDto> {
    return this.expensesClient.expenses_SubmitClaim(claimId, crypto.randomUUID());
  }

  decideApproval(claimId: string, request: DecideApprovalRequest): Observable<void> {
    return this.expensesClient.expenses_DecideApproval(claimId, crypto.randomUUID(), request);
  }

  createPolicy(request: CreateExpensePolicyRequest): Observable<CreateExpensePolicyResponse> {
    return this.policiesClient.expensePolicies_Create(crypto.randomUUID(), request);
  }

  settleClaim(request: SettleExpenseClaimRequest): Observable<void> {
    return this.settlementsClient.expenseSettlements_Settle(crypto.randomUUID(), request);
  }
}
