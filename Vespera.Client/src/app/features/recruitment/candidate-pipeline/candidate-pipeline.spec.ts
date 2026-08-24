import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { RecruitmentService } from '../recruitment.service';
import { CandidatePipelineDto, CandidateStatus } from '../recruitment.models';
import { CandidatePipeline } from './candidate-pipeline';

function pipeline(): CandidatePipelineDto {
  return {
    stages: [
      { id: 'stage-1', name: 'Screening', sequenceNumber: 0 },
      { id: 'stage-2', name: 'Offer', sequenceNumber: 1 },
    ],
    candidates: [{ id: 'cand-1', fullName: 'Asha Rao', status: CandidateStatus.InPipeline, currentPipelineStageId: 'stage-1' }],
  };
}

describe('CandidatePipeline', () => {
  let recruitmentSpy: jasmine.SpyObj<RecruitmentService>;

  async function setUp() {
    recruitmentSpy = jasmine.createSpyObj<RecruitmentService>('RecruitmentService', ['getPipeline', 'moveToStage']);
    recruitmentSpy.getPipeline.and.returnValue(of(pipeline()));
    recruitmentSpy.moveToStage.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [CandidatePipeline],
      providers: [
        provideRouter([]),
        { provide: RecruitmentService, useValue: recruitmentSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'req-1' }) } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(CandidatePipeline);
    fixture.detectChanges();
    return fixture;
  }

  it('moves a candidate to the target stage locally when the server accepts the move', async () => {
    const fixture = await setUp();
    const component = fixture.componentInstance;

    component.moveCandidate('cand-1', 'stage-2');

    expect(recruitmentSpy.moveToStage).toHaveBeenCalledWith('cand-1', { targetStageId: 'stage-2' });
    expect(component.pipeline()!.candidates[0].currentPipelineStageId).toBe('stage-2');
  });

  it('reverts the candidate to its original stage when the server rejects the move', async () => {
    const fixture = await setUp();
    recruitmentSpy.moveToStage.and.returnValue(
      throwError(() => ({ error: { detail: 'Cannot move to Offer without a completed interview.' } })),
    );
    const component = fixture.componentInstance;

    component.moveCandidate('cand-1', 'stage-2');

    // The optimistic update happens synchronously before the (also synchronous, in this test) error
    // fires — the net effect after the observable settles must be a revert to the original stage.
    expect(component.pipeline()!.candidates[0].currentPipelineStageId).toBe('stage-1');
    expect(recruitmentSpy.moveToStage).toHaveBeenCalledWith('cand-1', { targetStageId: 'stage-2' });
  });

  it('does nothing for an unknown candidate id', async () => {
    const fixture = await setUp();
    const component = fixture.componentInstance;
    const before = component.pipeline();

    component.moveCandidate('does-not-exist', 'stage-2');

    expect(component.pipeline()!.candidates[0].currentPipelineStageId).toBe(before!.candidates[0].currentPipelineStageId);
  });
});
