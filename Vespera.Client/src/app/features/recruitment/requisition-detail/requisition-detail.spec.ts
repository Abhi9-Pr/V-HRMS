import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { RecruitmentService } from '../recruitment.service';
import { JobRequisitionDto, JobRequisitionStatus, RequisitionApprovalStatus } from '../recruitment.models';
import { RequisitionDetail } from './requisition-detail';

function requisition(overrides: Partial<JobRequisitionDto>): JobRequisitionDto {
  return {
    id: 'req-1',
    title: 'Engineer',
    departmentId: 'dept-1',
    openingsCount: 2,
    status: JobRequisitionStatus.Open,
    approvalStatus: RequisitionApprovalStatus.Draft,
    isPublished: false,
    rejectionReason: null,
    stages: [],
    ...overrides,
  };
}

describe('RequisitionDetail', () => {
  let recruitmentSpy: jasmine.SpyObj<RecruitmentService>;

  function setUp(dto: JobRequisitionDto) {
    recruitmentSpy = jasmine.createSpyObj<RecruitmentService>('RecruitmentService', ['getRequisitionById']);
    recruitmentSpy.getRequisitionById.and.returnValue(of(dto));

    return TestBed.configureTestingModule({
      imports: [RequisitionDetail],
      providers: [
        provideRouter([]),
        { provide: RecruitmentService, useValue: recruitmentSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: dto.id }) } },
        },
      ],
    }).compileComponents();
  }

  it('shows only Submit for Approval for a Draft requisition', async () => {
    await setUp(requisition({ approvalStatus: RequisitionApprovalStatus.Draft }));
    const fixture = TestBed.createComponent(RequisitionDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const dto = component.requisition()!;

    expect(component.canSubmitForApproval(dto)).toBeTrue();
    expect(component.canDecide(dto)).toBeFalse();
    expect(component.canPublish(dto)).toBeFalse();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).toContain('action-submit-for-approval');
    expect(testIds).not.toContain('action-decide');
    expect(testIds).not.toContain('action-publish');
  });

  it('shows only Approve/Reject for a PendingApproval requisition', async () => {
    await setUp(requisition({ approvalStatus: RequisitionApprovalStatus.PendingApproval }));
    const fixture = TestBed.createComponent(RequisitionDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const dto = component.requisition()!;

    expect(component.canSubmitForApproval(dto)).toBeFalse();
    expect(component.canDecide(dto)).toBeTrue();
    expect(component.canPublish(dto)).toBeFalse();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).not.toContain('action-submit-for-approval');
    expect(testIds).toContain('action-decide');
    expect(testIds).not.toContain('action-publish');
  });

  it('shows Publish only when Approved, Open, and not yet published', async () => {
    await setUp(
      requisition({ approvalStatus: RequisitionApprovalStatus.Approved, status: JobRequisitionStatus.Open, isPublished: false }),
    );
    const fixture = TestBed.createComponent(RequisitionDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const dto = component.requisition()!;

    expect(component.canPublish(dto)).toBeTrue();

    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).toContain('action-publish');
  });

  it('does not show Publish once already published', async () => {
    await setUp(
      requisition({ approvalStatus: RequisitionApprovalStatus.Approved, status: JobRequisitionStatus.Open, isPublished: true }),
    );
    const fixture = TestBed.createComponent(RequisitionDetail);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const dto = component.requisition()!;

    expect(component.canPublish(dto)).toBeFalse();
    const testIds = Array.from(fixture.nativeElement.querySelectorAll('[data-testid]')).map((el: any) => el.getAttribute('data-testid'));
    expect(testIds).not.toContain('action-publish');
  });
});
