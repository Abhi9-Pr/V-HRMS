import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { RecruitmentFacade } from './recruitment.facade';
import {
  ApiError,
  CandidatesClient,
  InterviewsClient,
  OffersClient,
  PublicJobsClient,
  RequisitionsClient,
} from 'vespera-shared';

describe('RecruitmentFacade', () => {
  let requisitionsClient: jest.Mocked<
    Pick<
      RequisitionsClient,
      | 'requisitions_GetAll'
      | 'requisitions_GetById'
      | 'requisitions_GetPipeline'
      | 'requisitions_Create'
      | 'requisitions_AddStage'
      | 'requisitions_SubmitForApproval'
      | 'requisitions_DecideApproval'
      | 'requisitions_Publish'
    >
  >;
  let candidatesClient: jest.Mocked<
    Pick<
      CandidatesClient,
      | 'candidates_GetById'
      | 'candidates_Create'
      | 'candidates_MoveToStage'
      | 'candidates_Reject'
      | 'candidates_Withdraw'
    >
  >;
  let interviewsClient: jest.Mocked<
    Pick<
      InterviewsClient,
      | 'interviews_GetForCandidate'
      | 'interviews_Schedule'
      | 'interviews_SubmitScorecard'
      | 'interviews_Complete'
      | 'interviews_Cancel'
      | 'interviews_Reschedule'
    >
  >;
  let offersClient: jest.Mocked<
    Pick<
      OffersClient,
      | 'offers_GetForCandidate'
      | 'offers_Create'
      | 'offers_Send'
      | 'offers_Accept'
      | 'offers_Decline'
      | 'offers_Withdraw'
      | 'offers_DownloadPdf'
      | 'offers_ConvertToEmployee'
    >
  >;
  let publicJobsClient: jest.Mocked<Pick<PublicJobsClient, 'publicJobs_GetPublicJobs'>>;
  let facade: RecruitmentFacade;

  beforeEach(() => {
    requisitionsClient = {
      requisitions_GetAll: jest.fn(),
      requisitions_GetById: jest.fn(),
      requisitions_GetPipeline: jest.fn(),
      requisitions_Create: jest.fn(),
      requisitions_AddStage: jest.fn(),
      requisitions_SubmitForApproval: jest.fn(),
      requisitions_DecideApproval: jest.fn(),
      requisitions_Publish: jest.fn(),
    };
    candidatesClient = {
      candidates_GetById: jest.fn(),
      candidates_Create: jest.fn(),
      candidates_MoveToStage: jest.fn(),
      candidates_Reject: jest.fn(),
      candidates_Withdraw: jest.fn(),
    };
    interviewsClient = {
      interviews_GetForCandidate: jest.fn(),
      interviews_Schedule: jest.fn(),
      interviews_SubmitScorecard: jest.fn(),
      interviews_Complete: jest.fn(),
      interviews_Cancel: jest.fn(),
      interviews_Reschedule: jest.fn(),
    };
    offersClient = {
      offers_GetForCandidate: jest.fn(),
      offers_Create: jest.fn(),
      offers_Send: jest.fn(),
      offers_Accept: jest.fn(),
      offers_Decline: jest.fn(),
      offers_Withdraw: jest.fn(),
      offers_DownloadPdf: jest.fn(),
      offers_ConvertToEmployee: jest.fn(),
    };
    publicJobsClient = { publicJobs_GetPublicJobs: jest.fn() };

    TestBed.configureTestingModule({
      providers: [
        RecruitmentFacade,
        { provide: RequisitionsClient, useValue: requisitionsClient },
        { provide: CandidatesClient, useValue: candidatesClient },
        { provide: InterviewsClient, useValue: interviewsClient },
        { provide: OffersClient, useValue: offersClient },
        { provide: PublicJobsClient, useValue: publicJobsClient },
      ],
    });

    facade = TestBed.inject(RecruitmentFacade);
  });

  it('loadRequisitions() should populate requisitions/totalCount on success', () => {
    requisitionsClient.requisitions_GetAll.mockReturnValue(
      of({ items: [{ id: 'r1', title: 'Engineer' }], page: 1, pageSize: 20, totalCount: 1 } as never),
    );

    facade.loadRequisitions({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.requisitionsLoading()).toBe(false);
    expect(facade.requisitions()).toHaveLength(1);
    expect(facade.requisitionsTotalCount()).toBe(1);
    expect(requisitionsClient.requisitions_GetAll).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('loadRequisitions() should populate error on failure', () => {
    const apiError: ApiError = { status: 500, code: 'server_error', message: 'boom' };
    requisitionsClient.requisitions_GetAll.mockReturnValue(throwError(() => apiError));

    facade.loadRequisitions({ page: 1, pageSize: 20, sortDescending: false });

    expect(facade.requisitionsLoading()).toBe(false);
    expect(facade.requisitionsError()).toEqual(apiError);
  });

  it('loadRequisitionById() should populate requisition on success', () => {
    requisitionsClient.requisitions_GetById.mockReturnValue(of({ id: 'r1' } as never));

    facade.loadRequisitionById('r1');

    expect(facade.requisition()?.id).toBe('r1');
    expect(requisitionsClient.requisitions_GetById).toHaveBeenCalledWith('r1');
  });

  it('loadPipeline() should populate pipeline on success', () => {
    requisitionsClient.requisitions_GetPipeline.mockReturnValue(of({ stages: [], candidates: [] } as never));

    facade.loadPipeline('r1');

    expect(facade.pipeline()).toEqual({ stages: [], candidates: [] });
    expect(requisitionsClient.requisitions_GetPipeline).toHaveBeenCalledWith('r1');
  });

  it('loadCandidateById() should populate candidate on success', () => {
    candidatesClient.candidates_GetById.mockReturnValue(of({ id: 'c1' } as never));

    facade.loadCandidateById('c1');

    expect(facade.candidate()?.id).toBe('c1');
  });

  it('loadInterviewsForCandidate() should populate interviews on success', () => {
    interviewsClient.interviews_GetForCandidate.mockReturnValue(of([{ id: 'i1' }] as never));

    facade.loadInterviewsForCandidate('c1');

    expect(facade.interviews()).toHaveLength(1);
  });

  it('loadOffersForCandidate() should populate offers on success', () => {
    offersClient.offers_GetForCandidate.mockReturnValue(of([{ id: 'o1' }] as never));

    facade.loadOffersForCandidate('c1');

    expect(facade.offers()).toHaveLength(1);
  });

  it('loadPublicJobs() should populate publicJobs on success', () => {
    publicJobsClient.publicJobs_GetPublicJobs.mockReturnValue(of([{ id: 'j1', title: 'Engineer' }] as never));

    facade.loadPublicJobs();

    expect(facade.publicJobs()).toHaveLength(1);
  });

  it('createRequisition() should delegate to the generated client with a fresh idempotency key', (done) => {
    requisitionsClient.requisitions_Create.mockReturnValue(of({ id: 'r1' } as never));

    facade.createRequisition({ title: 'Engineer', departmentId: 'd1', openingsCount: 2 }).subscribe((result) => {
      expect(result.id).toBe('r1');
      const [idempotencyKey, body] = requisitionsClient.requisitions_Create.mock.calls[0];
      expect(typeof idempotencyKey).toBe('string');
      expect(idempotencyKey!.length).toBeGreaterThan(0);
      expect(body).toEqual({ title: 'Engineer', departmentId: 'd1', openingsCount: 2 });
      done();
    });
  });

  it('addStage() should delegate to the generated client', (done) => {
    requisitionsClient.requisitions_AddStage.mockReturnValue(of(undefined));

    facade.addStage('r1', { stageName: 'Screening' }).subscribe(() => {
      const [requisitionId, , body] = requisitionsClient.requisitions_AddStage.mock.calls[0];
      expect(requisitionId).toBe('r1');
      expect(body).toEqual({ stageName: 'Screening' });
      done();
    });
  });

  it('submitForApproval() should delegate to the generated client', (done) => {
    requisitionsClient.requisitions_SubmitForApproval.mockReturnValue(of(undefined));

    facade.submitForApproval('r1').subscribe(() => {
      expect(requisitionsClient.requisitions_SubmitForApproval).toHaveBeenCalledWith('r1', expect.any(String));
      done();
    });
  });

  it('decideApproval() should delegate to the generated client', (done) => {
    requisitionsClient.requisitions_DecideApproval.mockReturnValue(of(undefined));

    facade.decideApproval('r1', { approved: true, comment: 'ok' }).subscribe(() => {
      const [requisitionId, , body] = requisitionsClient.requisitions_DecideApproval.mock.calls[0];
      expect(requisitionId).toBe('r1');
      expect(body).toEqual({ approved: true, comment: 'ok' });
      done();
    });
  });

  it('publish() should delegate to the generated client', (done) => {
    requisitionsClient.requisitions_Publish.mockReturnValue(of(undefined));

    facade.publish('r1').subscribe(() => {
      expect(requisitionsClient.requisitions_Publish).toHaveBeenCalledWith('r1', expect.any(String));
      done();
    });
  });

  it('createCandidate() should delegate to the generated client', (done) => {
    candidatesClient.candidates_Create.mockReturnValue(of({ id: 'c1' } as never));

    facade
      .createCandidate({ jobRequisitionId: 'r1', fullName: 'Jane Doe', email: 'jane@example.com', phone: '1234567890' })
      .subscribe((result) => {
        expect(result.id).toBe('c1');
        done();
      });
  });

  it('moveToStage() should delegate to the generated client', (done) => {
    candidatesClient.candidates_MoveToStage.mockReturnValue(of(undefined));

    facade.moveToStage('c1', { targetStageId: 's1' }).subscribe(() => {
      const [candidateId, , body] = candidatesClient.candidates_MoveToStage.mock.calls[0];
      expect(candidateId).toBe('c1');
      expect(body).toEqual({ targetStageId: 's1' });
      done();
    });
  });

  it('scheduleInterview() should delegate to the generated client', (done) => {
    interviewsClient.interviews_Schedule.mockReturnValue(of({ id: 'i1' } as never));

    facade
      .scheduleInterview({
        candidateId: 'c1',
        pipelineStageId: 's1',
        scheduledAt: new Date('2026-01-01'),
        interviewerIds: ['e1'],
      })
      .subscribe((result) => {
        expect(result.id).toBe('i1');
        done();
      });
  });

  it('submitScorecard() should delegate to the generated client', (done) => {
    interviewsClient.interviews_SubmitScorecard.mockReturnValue(of(undefined));

    facade.submitScorecard('i1', { interviewerId: 'e1', rating: 4, notes: 'good' }).subscribe(() => {
      const [interviewId, , body] = interviewsClient.interviews_SubmitScorecard.mock.calls[0];
      expect(interviewId).toBe('i1');
      expect(body).toEqual({ interviewerId: 'e1', rating: 4, notes: 'good' });
      done();
    });
  });

  it('createOffer() should delegate to the generated client', (done) => {
    offersClient.offers_Create.mockReturnValue(of({ id: 'o1' } as never));

    facade
      .createOffer({
        candidateId: 'c1',
        proposedDesignationId: 'd1',
        proposedCtc: 1000000,
        currency: 0,
        joiningDate: new Date('2026-02-01'),
      })
      .subscribe((result) => {
        expect(result.id).toBe('o1');
        done();
      });
  });

  it('convertToEmployee() should delegate to the generated client', (done) => {
    offersClient.offers_ConvertToEmployee.mockReturnValue(of({ id: 'emp-1' } as never));

    facade
      .convertToEmployee({
        candidateId: 'c1',
        offerLetterId: 'o1',
        employeeCode: 'EMP-100',
        dateOfBirth: new Date('2000-01-01'),
        locationId: 'l1',
      })
      .subscribe((result) => {
        expect(result.id).toBe('emp-1');
        done();
      });
  });

  it('downloadOfferPdf() should delegate to the generated client', (done) => {
    offersClient.offers_DownloadPdf.mockReturnValue(of({ fileName: 'offer.pdf' } as never));

    facade.downloadOfferPdf('o1').subscribe((result) => {
      expect(result.fileName).toBe('offer.pdf');
      expect(offersClient.offers_DownloadPdf).toHaveBeenCalledWith('o1');
      done();
    });
  });
});
