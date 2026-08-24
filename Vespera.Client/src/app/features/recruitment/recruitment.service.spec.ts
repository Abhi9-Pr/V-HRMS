import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Currency } from '../../core/models/currency';
import { RecruitmentService } from './recruitment.service';

describe('RecruitmentService', () => {
  let service: RecruitmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RecruitmentService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RecruitmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createRequisition posts to /recruitment/requisitions with an Idempotency-Key header', () => {
    service.createRequisition({ title: 'Engineer', departmentId: 'dept-1', openingsCount: 2 }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ title: 'Engineer', departmentId: 'dept-1', openingsCount: 2 });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'req-1' });
  });

  it('addStage posts to the stages sub-route', () => {
    service.addStage('req-1', { stageName: 'Screening' }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions/req-1/stages');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ stageName: 'Screening' });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('submitForApproval posts an empty body with an Idempotency-Key header', () => {
    service.submitForApproval('req-1').subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions/req-1/submit');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('decideRequisitionApproval posts the decision body', () => {
    service.decideRequisitionApproval('req-1', { approved: true, comment: 'Looks good' }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions/req-1/decision');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ approved: true, comment: 'Looks good' });
    req.flush(null);
  });

  it('publish posts to the publish sub-route', () => {
    service.publish('req-1').subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions/req-1/publish');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('getRequisitions sends paging as query params', () => {
    service.getRequisitions({ page: 2, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/v1/recruitment/requisitions');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false });
  });

  it('getPipeline fetches the Kanban board data for a requisition', () => {
    service.getPipeline('req-1').subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/requisitions/req-1/pipeline');
    expect(req.request.method).toBe('GET');
    req.flush({ stages: [], candidates: [] });
  });

  it('createCandidate posts to /recruitment/candidates', () => {
    service.createCandidate({ jobRequisitionId: 'req-1', fullName: 'Asha Rao', email: 'asha@example.com', phone: '+911234567890' }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/candidates');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'cand-1' });
  });

  it('moveToStage posts the target stage id', () => {
    service.moveToStage('cand-1', { targetStageId: 'stage-2' }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/candidates/cand-1/stage');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ targetStageId: 'stage-2' });
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush(null);
  });

  it('scheduleInterview posts to /recruitment/interviews', () => {
    service
      .scheduleInterview({
        candidateId: 'cand-1',
        pipelineStageId: 'stage-1',
        scheduledAt: '2026-02-01T10:00:00Z',
        interviewerIds: ['emp-1', 'emp-2'],
      })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/interviews');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.interviewerIds).toEqual(['emp-1', 'emp-2']);
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'iv-1' });
  });

  it('submitScorecard posts to the scorecards sub-route', () => {
    service.submitScorecard('iv-1', { interviewerId: 'emp-1', rating: 4, notes: 'Strong' }).subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/interviews/iv-1/scorecards');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ interviewerId: 'emp-1', rating: 4, notes: 'Strong' });
    req.flush(null);
  });

  it('getInterviewsForCandidate hits the by-candidate route', () => {
    service.getInterviewsForCandidate('cand-1').subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/interviews/by-candidate/cand-1');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('createOffer posts the numeric Currency ordinal', () => {
    service
      .createOffer({ candidateId: 'cand-1', proposedDesignationId: 'desig-1', proposedCtc: 1200000, currency: Currency.Inr, joiningDate: '2026-03-01' })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/offers');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.currency).toBe(Currency.Inr);
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'offer-1' });
  });

  it('downloadOfferPdf requests a blob response', () => {
    service.downloadOfferPdf('offer-1').subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/offers/offer-1/pdf');
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['%PDF-1.4']));
  });

  it('convertToEmployee posts to the convert-to-employee route', () => {
    service
      .convertToEmployee({ candidateId: 'cand-1', offerLetterId: 'offer-1', employeeCode: 'EMP-100', dateOfBirth: '1995-01-01', locationId: 'loc-1' })
      .subscribe();

    const req = httpMock.expectOne('/api/v1/recruitment/offers/convert-to-employee');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('Idempotency-Key')).toBeTrue();
    req.flush({ id: 'emp-1' });
  });

  it('getPublicJobs sends the X-Tenant-Id header explicitly and no Idempotency-Key (it is a GET)', () => {
    service.getPublicJobs('tenant-abc').subscribe();

    const req = httpMock.expectOne('/api/v1/public/jobs');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('X-Tenant-Id')).toBe('tenant-abc');
    expect(req.request.headers.has('Idempotency-Key')).toBeFalse();
    req.flush([{ id: 'job-1', title: 'Engineer', departmentName: 'Engineering', openingsCount: 2 }]);
  });
});
