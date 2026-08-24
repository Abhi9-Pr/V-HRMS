import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { idempotencyHeaders } from '../../core/http/idempotency';
import {
  AddStageRequest,
  CandidateDto,
  CandidatePipelineDto,
  CompleteInterviewRequest,
  ConvertToEmployeeRequest,
  CreateCandidateRequest,
  CreateOfferRequest,
  CreateRequisitionRequest,
  DecideRequisitionApprovalRequest,
  InterviewDto,
  JobRequisitionDto,
  MoveToStageRequest,
  OfferLetterDto,
  PagedRequest,
  PagedResult,
  PublicJobDto,
  RescheduleInterviewRequest,
  ScheduleInterviewRequest,
  SubmitScorecardRequest,
} from './recruitment.models';

@Injectable({ providedIn: 'root' })
export class RecruitmentService {
  private readonly requisitionsUrl = `${environment.apiBaseUrl}/recruitment/requisitions`;
  private readonly candidatesUrl = `${environment.apiBaseUrl}/recruitment/candidates`;
  private readonly interviewsUrl = `${environment.apiBaseUrl}/recruitment/interviews`;
  private readonly offersUrl = `${environment.apiBaseUrl}/recruitment/offers`;
  private readonly publicJobsUrl = `${environment.apiBaseUrl}/public/jobs`;

  constructor(private readonly http: HttpClient) {}

  // Requisitions

  createRequisition(request: CreateRequisitionRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.requisitionsUrl, request, { headers: idempotencyHeaders() });
  }

  addStage(requisitionId: string, request: AddStageRequest): Observable<void> {
    return this.http.post<void>(`${this.requisitionsUrl}/${requisitionId}/stages`, request, {
      headers: idempotencyHeaders(),
    });
  }

  submitForApproval(requisitionId: string): Observable<void> {
    return this.http.post<void>(`${this.requisitionsUrl}/${requisitionId}/submit`, {}, { headers: idempotencyHeaders() });
  }

  decideRequisitionApproval(requisitionId: string, request: DecideRequisitionApprovalRequest): Observable<void> {
    return this.http.post<void>(`${this.requisitionsUrl}/${requisitionId}/decision`, request, {
      headers: idempotencyHeaders(),
    });
  }

  publish(requisitionId: string): Observable<void> {
    return this.http.post<void>(`${this.requisitionsUrl}/${requisitionId}/publish`, {}, { headers: idempotencyHeaders() });
  }

  getRequisitions(paging: PagedRequest): Observable<PagedResult<JobRequisitionDto>> {
    return this.http.get<PagedResult<JobRequisitionDto>>(this.requisitionsUrl, { params: toHttpParams(paging) });
  }

  getRequisitionById(id: string): Observable<JobRequisitionDto> {
    return this.http.get<JobRequisitionDto>(`${this.requisitionsUrl}/${id}`);
  }

  getPipeline(requisitionId: string): Observable<CandidatePipelineDto> {
    return this.http.get<CandidatePipelineDto>(`${this.requisitionsUrl}/${requisitionId}/pipeline`);
  }

  // Candidates

  createCandidate(request: CreateCandidateRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.candidatesUrl, request, { headers: idempotencyHeaders() });
  }

  moveToStage(candidateId: string, request: MoveToStageRequest): Observable<void> {
    return this.http.post<void>(`${this.candidatesUrl}/${candidateId}/stage`, request, { headers: idempotencyHeaders() });
  }

  rejectCandidate(candidateId: string): Observable<void> {
    return this.http.post<void>(`${this.candidatesUrl}/${candidateId}/reject`, {}, { headers: idempotencyHeaders() });
  }

  withdrawCandidate(candidateId: string): Observable<void> {
    return this.http.post<void>(`${this.candidatesUrl}/${candidateId}/withdraw`, {}, { headers: idempotencyHeaders() });
  }

  getCandidateById(id: string): Observable<CandidateDto> {
    return this.http.get<CandidateDto>(`${this.candidatesUrl}/${id}`);
  }

  // Interviews

  scheduleInterview(request: ScheduleInterviewRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.interviewsUrl, request, { headers: idempotencyHeaders() });
  }

  submitScorecard(interviewId: string, request: SubmitScorecardRequest): Observable<void> {
    return this.http.post<void>(`${this.interviewsUrl}/${interviewId}/scorecards`, request, {
      headers: idempotencyHeaders(),
    });
  }

  completeInterview(interviewId: string, request: CompleteInterviewRequest): Observable<void> {
    return this.http.post<void>(`${this.interviewsUrl}/${interviewId}/complete`, request, {
      headers: idempotencyHeaders(),
    });
  }

  cancelInterview(interviewId: string): Observable<void> {
    return this.http.post<void>(`${this.interviewsUrl}/${interviewId}/cancel`, {}, { headers: idempotencyHeaders() });
  }

  rescheduleInterview(interviewId: string, request: RescheduleInterviewRequest): Observable<void> {
    return this.http.post<void>(`${this.interviewsUrl}/${interviewId}/reschedule`, request, {
      headers: idempotencyHeaders(),
    });
  }

  getInterviewsForCandidate(candidateId: string): Observable<InterviewDto[]> {
    return this.http.get<InterviewDto[]>(`${this.interviewsUrl}/by-candidate/${candidateId}`);
  }

  // Offers

  createOffer(request: CreateOfferRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.offersUrl, request, { headers: idempotencyHeaders() });
  }

  sendOffer(offerLetterId: string): Observable<void> {
    return this.http.post<void>(`${this.offersUrl}/${offerLetterId}/send`, {}, { headers: idempotencyHeaders() });
  }

  acceptOffer(offerLetterId: string): Observable<void> {
    return this.http.post<void>(`${this.offersUrl}/${offerLetterId}/accept`, {}, { headers: idempotencyHeaders() });
  }

  declineOffer(offerLetterId: string): Observable<void> {
    return this.http.post<void>(`${this.offersUrl}/${offerLetterId}/decline`, {}, { headers: idempotencyHeaders() });
  }

  withdrawOffer(offerLetterId: string): Observable<void> {
    return this.http.post<void>(`${this.offersUrl}/${offerLetterId}/withdraw`, {}, { headers: idempotencyHeaders() });
  }

  downloadOfferPdf(offerLetterId: string): Observable<Blob> {
    return this.http.get(`${this.offersUrl}/${offerLetterId}/pdf`, { responseType: 'blob' });
  }

  getOfferById(id: string): Observable<OfferLetterDto> {
    return this.http.get<OfferLetterDto>(`${this.offersUrl}/${id}`);
  }

  getOffersForCandidate(candidateId: string): Observable<OfferLetterDto[]> {
    return this.http.get<OfferLetterDto[]>(`${this.offersUrl}/by-candidate/${candidateId}`);
  }

  convertToEmployee(request: ConvertToEmployeeRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.offersUrl}/convert-to-employee`, request, {
      headers: idempotencyHeaders(),
    });
  }

  // Public careers page — no auth token required; tenant identity comes from an explicit header
  // rather than AuthService's signal, since an anonymous visitor has no session at all (see
  // HttpTenantContext: it falls back to X-Tenant-Id whenever there's no bearer token).
  getPublicJobs(tenantId: string): Observable<PublicJobDto[]> {
    return this.http.get<PublicJobDto[]>(this.publicJobsUrl, { headers: { 'X-Tenant-Id': tenantId } });
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
