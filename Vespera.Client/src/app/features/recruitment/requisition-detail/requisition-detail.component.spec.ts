import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { JobRequisitionDto, JobRequisitionStatus, RequisitionApprovalStatus } from '../../../core/api/generated/api-client';
import { AuthService } from '../../../core/auth/auth.service';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { RequisitionDetailComponent } from './requisition-detail.component';

describe('RequisitionDetailComponent', () => {
  let fixture: ComponentFixture<RequisitionDetailComponent>;
  let facade: jest.Mocked<Pick<RecruitmentFacade, 'loadRequisitionById' | 'requisition' | 'requisitionLoading' | 'requisitionError'>>;

  function setup(requisition: JobRequisitionDto): void {
    facade = {
      loadRequisitionById: jest.fn(),
      requisition: signal(requisition),
      requisitionLoading: signal(false),
      requisitionError: signal(null),
    } as never;

    TestBed.configureTestingModule({
      imports: [RequisitionDetailComponent],
      providers: [
        provideNoopAnimations(),
        { provide: RecruitmentFacade, useValue: facade },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: requisition.id }) } } },
        {
          provide: AuthService,
          useValue: { permissions: signal(['Recruitment.ManageRequisitions', 'Recruitment.ApproveRequisitions']) },
        },
      ],
    });

    fixture = TestBed.createComponent(RequisitionDetailComponent);
    fixture.detectChanges();
  }

  it('should load the requisition on init', () => {
    setup({ id: 'r1', approvalStatus: RequisitionApprovalStatus._0 });

    expect(facade.loadRequisitionById).toHaveBeenCalledWith('r1');
  });

  it('a Draft requisition should only show the submit-for-approval action', () => {
    setup({ id: 'r1', approvalStatus: RequisitionApprovalStatus._0, status: JobRequisitionStatus._0, isPublished: false });

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map(
      (element: Element) => (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toEqual(['submit-for-approval-action']);
  });

  it('a PendingApproval requisition should only show approve/reject actions', () => {
    setup({ id: 'r1', approvalStatus: RequisitionApprovalStatus._1, status: JobRequisitionStatus._0, isPublished: false });

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map(
      (element: Element) => (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toEqual(expect.arrayContaining(['approve-action', 'reject-action']));
    expect(testIds).not.toContain('submit-for-approval-action');
    expect(testIds).not.toContain('publish-action');
  });

  it('an Approved, Open, unpublished requisition should only show the publish action', () => {
    setup({ id: 'r1', approvalStatus: RequisitionApprovalStatus._2, status: JobRequisitionStatus._0, isPublished: false });

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map(
      (element: Element) => (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toEqual(['publish-action']);
  });

  it('an already-published requisition should show no workflow actions', () => {
    setup({ id: 'r1', approvalStatus: RequisitionApprovalStatus._2, status: JobRequisitionStatus._0, isPublished: true });

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map(
      (element: Element) => (element as HTMLElement).getAttribute('data-testid'),
    );

    expect(testIds).toHaveLength(0);
  });
});
