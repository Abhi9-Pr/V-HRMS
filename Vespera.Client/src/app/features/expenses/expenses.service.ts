import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { idempotencyHeaders } from '../../core/http/idempotency';
import {
  AddLineRequest,
  CreateExpensePolicyRequest,
  DecideApprovalRequest,
  ExpenseClaimDto,
  ExpensePolicyDto,
  OpenClaimRequest,
  PagedRequest,
  PagedResult,
  SettleExpenseClaimRequest,
  SubmitExpenseClaimResultDto,
  UploadExpenseReceiptResultDto,
} from './expenses.models';

@Injectable({ providedIn: 'root' })
export class ExpensesService {
  private readonly baseUrl = `${environment.apiBaseUrl}/expenses`;
  private readonly financeUrl = `${environment.apiBaseUrl}/finance`;

  constructor(private readonly http: HttpClient) {}

  openClaim(request: OpenClaimRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/claims`, request, {
      headers: idempotencyHeaders(),
    });
  }

  uploadReceipt(claimId: string, file: File): Observable<UploadExpenseReceiptResultDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<UploadExpenseReceiptResultDto>(`${this.baseUrl}/claims/${claimId}/receipts`, formData, {
      headers: idempotencyHeaders(),
    });
  }

  addLine(claimId: string, request: AddLineRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/claims/${claimId}/lines`, request, {
      headers: idempotencyHeaders(),
    });
  }

  submitClaim(claimId: string): Observable<SubmitExpenseClaimResultDto> {
    return this.http.post<SubmitExpenseClaimResultDto>(
      `${this.baseUrl}/claims/${claimId}/submit`,
      {},
      { headers: idempotencyHeaders() },
    );
  }

  decideApproval(claimId: string, request: DecideApprovalRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/claims/${claimId}/decision`, request, {
      headers: idempotencyHeaders(),
    });
  }

  getMyClaims(paging: PagedRequest): Observable<PagedResult<ExpenseClaimDto>> {
    return this.http.get<PagedResult<ExpenseClaimDto>>(`${this.baseUrl}/claims`, {
      params: toHttpParams(paging),
    });
  }

  getClaimById(id: string): Observable<ExpenseClaimDto> {
    return this.http.get<ExpenseClaimDto>(`${this.baseUrl}/claims/${id}`);
  }

  getPendingApprovals(paging: PagedRequest): Observable<PagedResult<ExpenseClaimDto>> {
    return this.http.get<PagedResult<ExpenseClaimDto>>(`${this.baseUrl}/approvals/pending`, {
      params: toHttpParams(paging),
    });
  }

  getPolicies(paging: PagedRequest): Observable<PagedResult<ExpensePolicyDto>> {
    return this.http.get<PagedResult<ExpensePolicyDto>>(`${this.financeUrl}/expense-policies`, {
      params: toHttpParams(paging),
    });
  }

  createPolicy(request: CreateExpensePolicyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.financeUrl}/expense-policies`, request, {
      headers: idempotencyHeaders(),
    });
  }

  settleClaim(request: SettleExpenseClaimRequest): Observable<void> {
    return this.http.post<void>(`${this.financeUrl}/expense-settlements`, request, {
      headers: idempotencyHeaders(),
    });
  }
}

function toHttpParams(paging: PagedRequest): HttpParams {
  let params = new HttpParams();
  if (paging.page !== undefined) {
    params = params.set('page', paging.page);
  }
  if (paging.pageSize !== undefined) {
    params = params.set('pageSize', paging.pageSize);
  }
  if (paging.sortBy) {
    params = params.set('sortBy', paging.sortBy);
  }
  if (paging.sortDescending !== undefined) {
    params = params.set('sortDescending', paging.sortDescending);
  }
  return params;
}
