import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { CandidatePipelineComponent } from './candidate-pipeline.component';
import { ApiError, CandidateCardDto, CandidatePipelineDto } from 'vespera-shared';

describe('CandidatePipelineComponent', () => {
  let fixture: ComponentFixture<CandidatePipelineComponent>;
  let component: CandidatePipelineComponent;
  let facade: jest.Mocked<
    Pick<RecruitmentFacade, 'loadPipeline' | 'pipeline' | 'pipelineLoading' | 'pipelineError' | 'moveToStage'>
  >;

  function setup(pipeline: CandidatePipelineDto): void {
    facade = {
      loadPipeline: jest.fn(),
      pipeline: signal(pipeline),
      pipelineLoading: signal(false),
      pipelineError: signal(null),
      moveToStage: jest.fn(),
    } as never;

    TestBed.configureTestingModule({
      imports: [CandidatePipelineComponent],
      providers: [
        provideNoopAnimations(),
        { provide: RecruitmentFacade, useValue: facade },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'r1' }) } } },
      ],
    });

    fixture = TestBed.createComponent(CandidatePipelineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  const candidate: CandidateCardDto = { id: 'c1', fullName: 'Jane Doe', status: 0, currentPipelineStageId: undefined };

  it('should load the pipeline on init and bucket the candidate into the Unassigned column', () => {
    setup({ stages: [{ id: 's1', name: 'Screening', sequenceNumber: 0 }], candidates: [candidate] });

    expect(facade.loadPipeline).toHaveBeenCalledWith('r1');
    expect(component.columns()).toHaveLength(2);
    expect(component.columns()[0].id).toBe('__unassigned__');
    expect(component.columns()[0].candidates).toEqual([candidate]);
    expect(component.columns()[1].candidates).toEqual([]);
  });

  it('a successful drop should call moveToStage with the candidate and target stage ids', () => {
    setup({ stages: [{ id: 's1', name: 'Screening', sequenceNumber: 0 }], candidates: [candidate] });
    facade.moveToStage.mockReturnValue(of(undefined));

    const sourceColumn = component.columns()[0];
    const targetColumn = component.columns()[1];
    const event = {
      previousContainer: { data: sourceColumn.candidates },
      container: { data: targetColumn.candidates },
      previousIndex: 0,
      currentIndex: 0,
    } as CdkDragDrop<CandidateCardDto[]>;

    component.onDrop(event, 's1');

    expect(facade.moveToStage).toHaveBeenCalledWith('c1', { targetStageId: 's1' });
    expect(targetColumn.candidates).toEqual([candidate]);
    expect(sourceColumn.candidates).toEqual([]);
    expect(component.dropError()).toBeNull();
  });

  it('a failed drop should revert the candidate to its original column and surface the error', () => {
    setup({ stages: [{ id: 's1', name: 'Screening', sequenceNumber: 0 }], candidates: [candidate] });
    const apiError: ApiError = {
      status: 409,
      code: 'candidate.stage_transition_blocked',
      message: 'Requires a completed interview.',
    };
    facade.moveToStage.mockReturnValue(throwError(() => apiError));

    const sourceColumn = component.columns()[0];
    const targetColumn = component.columns()[1];
    const event = {
      previousContainer: { data: sourceColumn.candidates },
      container: { data: targetColumn.candidates },
      previousIndex: 0,
      currentIndex: 0,
    } as CdkDragDrop<CandidateCardDto[]>;

    component.onDrop(event, 's1');

    expect(sourceColumn.candidates).toEqual([candidate]);
    expect(targetColumn.candidates).toEqual([]);
    expect(component.dropError()).toBe('Requires a completed interview.');
  });

  it('a drop within the same column should do nothing', () => {
    setup({ stages: [{ id: 's1', name: 'Screening', sequenceNumber: 0 }], candidates: [candidate] });

    const column = component.columns()[0];
    const event = {
      previousContainer: { data: column.candidates },
      container: { data: column.candidates },
      previousIndex: 0,
      currentIndex: 0,
    } as CdkDragDrop<CandidateCardDto[]>;

    component.onDrop(event, '__unassigned__');

    expect(facade.moveToStage).not.toHaveBeenCalled();
  });
});
