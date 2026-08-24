import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { idempotencyHeaders } from '../../core/http/idempotency';
import {
  AddCommentRequest,
  AssignTicketRequest,
  CreatePublicHolidayRequest,
  CreateSlaPolicyRequest,
  CreateTicketCategoryRequest,
  PagedRequest,
  PagedResult,
  PublicHolidayDto,
  RaiseTicketRequest,
  RateSatisfactionRequest,
  SlaComplianceReportDto,
  SlaPolicyDto,
  TicketCategoryDto,
  TicketDto,
  TicketSummaryDto,
} from './helpdesk.models';

@Injectable({ providedIn: 'root' })
export class HelpdeskService {
  private readonly ticketsUrl = `${environment.apiBaseUrl}/helpdesk/tickets`;
  private readonly categoriesUrl = `${environment.apiBaseUrl}/helpdesk/ticket-categories`;
  private readonly slaPoliciesUrl = `${environment.apiBaseUrl}/helpdesk/sla-policies`;
  private readonly holidaysUrl = `${environment.apiBaseUrl}/helpdesk/public-holidays`;

  constructor(private readonly http: HttpClient) {}

  // Tickets

  raiseTicket(request: RaiseTicketRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.ticketsUrl, request, { headers: idempotencyHeaders() });
  }

  uploadAttachment(ticketId: string, file: File): Observable<{ reference: string }> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ reference: string }>(`${this.ticketsUrl}/${ticketId}/attachments`, formData, {
      headers: idempotencyHeaders(),
    });
  }

  addComment(ticketId: string, request: AddCommentRequest): Observable<void> {
    return this.http.post<void>(`${this.ticketsUrl}/${ticketId}/comments`, request, { headers: idempotencyHeaders() });
  }

  assignTicket(ticketId: string, request: AssignTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.ticketsUrl}/${ticketId}/assign`, request, { headers: idempotencyHeaders() });
  }

  resolveTicket(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.ticketsUrl}/${ticketId}/resolve`, {}, { headers: idempotencyHeaders() });
  }

  closeTicket(ticketId: string): Observable<void> {
    return this.http.post<void>(`${this.ticketsUrl}/${ticketId}/close`, {}, { headers: idempotencyHeaders() });
  }

  rateSatisfaction(ticketId: string, request: RateSatisfactionRequest): Observable<void> {
    return this.http.post<void>(`${this.ticketsUrl}/${ticketId}/satisfaction`, request, { headers: idempotencyHeaders() });
  }

  getTickets(paging: PagedRequest): Observable<PagedResult<TicketSummaryDto>> {
    return this.http.get<PagedResult<TicketSummaryDto>>(this.ticketsUrl, { params: toHttpParams(paging) });
  }

  getTicketById(id: string): Observable<TicketDto> {
    return this.http.get<TicketDto>(`${this.ticketsUrl}/${id}`);
  }

  getSlaComplianceReport(): Observable<SlaComplianceReportDto> {
    return this.http.get<SlaComplianceReportDto>(`${this.ticketsUrl}/sla-compliance-report`);
  }

  // Ticket categories

  createCategory(request: CreateTicketCategoryRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.categoriesUrl, request, { headers: idempotencyHeaders() });
  }

  getCategories(paging: PagedRequest): Observable<PagedResult<TicketCategoryDto>> {
    return this.http.get<PagedResult<TicketCategoryDto>>(this.categoriesUrl, { params: toHttpParams(paging) });
  }

  // SLA policies

  createSlaPolicy(request: CreateSlaPolicyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.slaPoliciesUrl, request, { headers: idempotencyHeaders() });
  }

  getSlaPolicies(paging: PagedRequest): Observable<PagedResult<SlaPolicyDto>> {
    return this.http.get<PagedResult<SlaPolicyDto>>(this.slaPoliciesUrl, { params: toHttpParams(paging) });
  }

  // Public holidays

  createHoliday(request: CreatePublicHolidayRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.holidaysUrl, request, { headers: idempotencyHeaders() });
  }

  getHolidays(paging: PagedRequest): Observable<PagedResult<PublicHolidayDto>> {
    return this.http.get<PagedResult<PublicHolidayDto>>(this.holidaysUrl, { params: toHttpParams(paging) });
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
