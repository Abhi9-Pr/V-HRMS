import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { DataTableQuery } from '../../../shared/data-table/data-table.model';
import {
  AddStageRequest,
  ApiError,
  CandidateDto,
  CandidatePipelineDto,
  CandidatesClient,
  CompleteInterviewRequest,
  ConvertToEmployeeRequest,
  ConvertToEmployeeResponse,
  CreateCandidateRequest,
  CreateCandidateResponse,
  CreateOfferRequest,
  CreateOfferResponse,
  CreateRequisitionRequest,
  CreateRequisitionResponse,
  DecideRequisitionApprovalRequest,
  FileResponse,
  InterviewDto,
  InterviewsClient,
  JobRequisitionDto,
  MoveToStageRequest,
  OfferLetterDto,
  OffersClient,
  PublicJobDto,
  PublicJobsClient,
  RequisitionsClient,
  RescheduleInterviewRequest,
  ScheduleInterviewRequest,
  ScheduleInterviewResponse,
  SubmitScorecardRequest,
} from 'vespera-shared';

/**
 * Wraps RequisitionsClient/CandidatesClient/InterviewsClient/OffersClient/PublicJobsClient
 * behind signals + plain methods, same split as ExpensesFacade/AssetsFacade (see
 * docs/CONTRIBUTING-frontend.md / docs/frontend-state.md): `load*` methods manage their own
 * loading/error signals and subscribe internally, mutating methods delegate straight to the
 * generated client and return the Observable for the calling component to handle.
 */
@Injectable({ providedIn: 'root' })
export class RecruitmentFacade {
  private readonly requisitionsClient = inject(RequisitionsClient);
  private readonly candidatesClient = inject(CandidatesClient);
  private readonly interviewsClient = inject(InterviewsClient);
  private readonly offersClient = inject(OffersClient);
  private readonly publicJobsClient = inject(PublicJobsClient);

  private readonly requisitionsSignal = signal<JobRequisitionDto[]>([]);
  private readonly requisitionsTotalCountSignal = signal(0);
  private readonly requisitionsLoadingSignal = signal(false);
  private readonly requisitionsErrorSignal = signal<ApiError | null>(null);

  private readonly requisitionSignal = signal<JobRequisitionDto | null>(null);
  private readonly requisitionLoadingSignal = signal(false);
  private readonly requisitionErrorSignal = signal<ApiError | null>(null);

  private readonly pipelineSignal = signal<CandidatePipelineDto | null>(null);
  private readonly pipelineLoadingSignal = signal(false);
  private readonly pipelineErrorSignal = signal<ApiError | null>(null);

  private readonly candidateSignal = signal<CandidateDto | null>(null);
  private readonly candidateLoadingSignal = signal(false);
  private readonly candidateErrorSignal = signal<ApiError | null>(null);

  private readonly interviewsSignal = signal<InterviewDto[]>([]);
  private readonly interviewsLoadingSignal = signal(false);
  private readonly interviewsErrorSignal = signal<ApiError | null>(null);

  private readonly offersSignal = signal<OfferLetterDto[]>([]);
  private readonly offersLoadingSignal = signal(false);
  private readonly offersErrorSignal = signal<ApiError | null>(null);

  private readonly publicJobsSignal = signal<PublicJobDto[]>([]);
  private readonly publicJobsLoadingSignal = signal(false);
  private readonly publicJobsErrorSignal = signal<ApiError | null>(null);

  readonly requisitions = this.requisitionsSignal.asReadonly();
  readonly requisitionsTotalCount = this.requisitionsTotalCountSignal.asReadonly();
  readonly requisitionsLoading = this.requisitionsLoadingSignal.asReadonly();
  readonly requisitionsError = this.requisitionsErrorSignal.asReadonly();

  readonly requisition = this.requisitionSignal.asReadonly();
  readonly requisitionLoading = this.requisitionLoadingSignal.asReadonly();
  readonly requisitionError = this.requisitionErrorSignal.asReadonly();

  readonly pipeline = this.pipelineSignal.asReadonly();
  readonly pipelineLoading = this.pipelineLoadingSignal.asReadonly();
  readonly pipelineError = this.pipelineErrorSignal.asReadonly();

  readonly candidate = this.candidateSignal.asReadonly();
  readonly candidateLoading = this.candidateLoadingSignal.asReadonly();
  readonly candidateError = this.candidateErrorSignal.asReadonly();

  readonly interviews = this.interviewsSignal.asReadonly();
  readonly interviewsLoading = this.interviewsLoadingSignal.asReadonly();
  readonly interviewsError = this.interviewsErrorSignal.asReadonly();

  readonly offers = this.offersSignal.asReadonly();
  readonly offersLoading = this.offersLoadingSignal.asReadonly();
  readonly offersError = this.offersErrorSignal.asReadonly();

  readonly publicJobs = this.publicJobsSignal.asReadonly();
  readonly publicJobsLoading = this.publicJobsLoadingSignal.asReadonly();
  readonly publicJobsError = this.publicJobsErrorSignal.asReadonly();

  loadRequisitions(query: DataTableQuery): void {
    this.requisitionsLoadingSignal.set(true);
    this.requisitionsErrorSignal.set(null);

    this.requisitionsClient
      .requisitions_GetAll(query.page, query.pageSize, query.sortBy, query.sortDescending)
      .subscribe({
        next: (result) => {
          this.requisitionsSignal.set(result.items ?? []);
          this.requisitionsTotalCountSignal.set(result.totalCount ?? 0);
          this.requisitionsLoadingSignal.set(false);
        },
        error: (apiError: ApiError) => {
          this.requisitionsErrorSignal.set(apiError);
          this.requisitionsLoadingSignal.set(false);
        },
      });
  }

  loadRequisitionById(id: string): void {
    this.requisitionLoadingSignal.set(true);
    this.requisitionErrorSignal.set(null);

    this.requisitionsClient.requisitions_GetById(id).subscribe({
      next: (requisition) => {
        this.requisitionSignal.set(requisition);
        this.requisitionLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.requisitionErrorSignal.set(apiError);
        this.requisitionLoadingSignal.set(false);
      },
    });
  }

  loadPipeline(requisitionId: string): void {
    this.pipelineLoadingSignal.set(true);
    this.pipelineErrorSignal.set(null);

    this.requisitionsClient.requisitions_GetPipeline(requisitionId).subscribe({
      next: (pipeline) => {
        this.pipelineSignal.set(pipeline);
        this.pipelineLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.pipelineErrorSignal.set(apiError);
        this.pipelineLoadingSignal.set(false);
      },
    });
  }

  /** Optimistically overwritten by candidate-pipeline.component.ts before a drag-drop mutation
   * call, then rolled back on failure — see that component for the revert-on-failure flow this
   * setter exists for. */
  setPipeline(pipeline: CandidatePipelineDto): void {
    this.pipelineSignal.set(pipeline);
  }

  loadCandidateById(id: string): void {
    this.candidateLoadingSignal.set(true);
    this.candidateErrorSignal.set(null);

    this.candidatesClient.candidates_GetById(id).subscribe({
      next: (candidate) => {
        this.candidateSignal.set(candidate);
        this.candidateLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.candidateErrorSignal.set(apiError);
        this.candidateLoadingSignal.set(false);
      },
    });
  }

  loadInterviewsForCandidate(candidateId: string): void {
    this.interviewsLoadingSignal.set(true);
    this.interviewsErrorSignal.set(null);

    this.interviewsClient.interviews_GetForCandidate(candidateId).subscribe({
      next: (interviews) => {
        this.interviewsSignal.set(interviews ?? []);
        this.interviewsLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.interviewsErrorSignal.set(apiError);
        this.interviewsLoadingSignal.set(false);
      },
    });
  }

  loadOffersForCandidate(candidateId: string): void {
    this.offersLoadingSignal.set(true);
    this.offersErrorSignal.set(null);

    this.offersClient.offers_GetForCandidate(candidateId).subscribe({
      next: (offers) => {
        this.offersSignal.set(offers ?? []);
        this.offersLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.offersErrorSignal.set(apiError);
        this.offersLoadingSignal.set(false);
      },
    });
  }

  /** Requires TenantResolutionService.resolveByCode() to have populated the cached tenant id
   * first — see public-jobs.component.ts — so authInterceptor's unauthenticated fallback attaches
   * X-Tenant-Id to this call. */
  loadPublicJobs(): void {
    this.publicJobsLoadingSignal.set(true);
    this.publicJobsErrorSignal.set(null);

    this.publicJobsClient.publicJobs_GetPublicJobs().subscribe({
      next: (jobs) => {
        this.publicJobsSignal.set(jobs ?? []);
        this.publicJobsLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.publicJobsErrorSignal.set(apiError);
        this.publicJobsLoadingSignal.set(false);
      },
    });
  }

  createRequisition(request: CreateRequisitionRequest): Observable<CreateRequisitionResponse> {
    return this.requisitionsClient.requisitions_Create(crypto.randomUUID(), request);
  }

  addStage(requisitionId: string, request: AddStageRequest): Observable<void> {
    return this.requisitionsClient.requisitions_AddStage(requisitionId, crypto.randomUUID(), request);
  }

  submitForApproval(requisitionId: string): Observable<void> {
    return this.requisitionsClient.requisitions_SubmitForApproval(requisitionId, crypto.randomUUID());
  }

  decideApproval(requisitionId: string, request: DecideRequisitionApprovalRequest): Observable<void> {
    return this.requisitionsClient.requisitions_DecideApproval(requisitionId, crypto.randomUUID(), request);
  }

  publish(requisitionId: string): Observable<void> {
    return this.requisitionsClient.requisitions_Publish(requisitionId, crypto.randomUUID());
  }

  createCandidate(request: CreateCandidateRequest): Observable<CreateCandidateResponse> {
    return this.candidatesClient.candidates_Create(crypto.randomUUID(), request);
  }

  moveToStage(candidateId: string, request: MoveToStageRequest): Observable<void> {
    return this.candidatesClient.candidates_MoveToStage(candidateId, crypto.randomUUID(), request);
  }

  rejectCandidate(candidateId: string): Observable<void> {
    return this.candidatesClient.candidates_Reject(candidateId, crypto.randomUUID());
  }

  withdrawCandidate(candidateId: string): Observable<void> {
    return this.candidatesClient.candidates_Withdraw(candidateId, crypto.randomUUID());
  }

  scheduleInterview(request: ScheduleInterviewRequest): Observable<ScheduleInterviewResponse> {
    return this.interviewsClient.interviews_Schedule(crypto.randomUUID(), request);
  }

  submitScorecard(interviewId: string, request: SubmitScorecardRequest): Observable<void> {
    return this.interviewsClient.interviews_SubmitScorecard(interviewId, crypto.randomUUID(), request);
  }

  completeInterview(interviewId: string, request: CompleteInterviewRequest): Observable<void> {
    return this.interviewsClient.interviews_Complete(interviewId, crypto.randomUUID(), request);
  }

  cancelInterview(interviewId: string): Observable<void> {
    return this.interviewsClient.interviews_Cancel(interviewId, crypto.randomUUID());
  }

  rescheduleInterview(interviewId: string, request: RescheduleInterviewRequest): Observable<void> {
    return this.interviewsClient.interviews_Reschedule(interviewId, crypto.randomUUID(), request);
  }

  createOffer(request: CreateOfferRequest): Observable<CreateOfferResponse> {
    return this.offersClient.offers_Create(crypto.randomUUID(), request);
  }

  sendOffer(offerLetterId: string): Observable<void> {
    return this.offersClient.offers_Send(offerLetterId, crypto.randomUUID());
  }

  acceptOffer(offerLetterId: string): Observable<void> {
    return this.offersClient.offers_Accept(offerLetterId, crypto.randomUUID());
  }

  declineOffer(offerLetterId: string): Observable<void> {
    return this.offersClient.offers_Decline(offerLetterId, crypto.randomUUID());
  }

  withdrawOffer(offerLetterId: string): Observable<void> {
    return this.offersClient.offers_Withdraw(offerLetterId, crypto.randomUUID());
  }

  downloadOfferPdf(offerLetterId: string): Observable<FileResponse> {
    return this.offersClient.offers_DownloadPdf(offerLetterId);
  }

  convertToEmployee(request: ConvertToEmployeeRequest): Observable<ConvertToEmployeeResponse> {
    return this.offersClient.offers_ConvertToEmployee(crypto.randomUUID(), request);
  }
}
