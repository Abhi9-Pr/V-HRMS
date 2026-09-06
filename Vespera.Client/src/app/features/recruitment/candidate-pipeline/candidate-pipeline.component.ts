import { Component, OnInit, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CdkDrag, CdkDragDrop, CdkDropList, CdkDropListGroup, transferArrayItem } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { CANDIDATE_STATUS_LABELS } from '../recruitment.labels';
import { ApiError, CandidateCardDto, PipelineStageDto } from 'vespera-shared';

const UNASSIGNED_COLUMN_ID = '__unassigned__';

interface PipelineColumn {
  id: string;
  name: string;
  candidates: CandidateCardDto[];
}

/**
 * The Kanban board. Columns are rebuilt from RecruitmentFacade.pipeline() into local mutable
 * arrays (`columns`) that Angular CDK's cdkDropList/transferArrayItem can move cards between
 * directly — a re-derived computed signal wouldn't give CDK a stable array reference to mutate.
 * A drop optimistically moves the card, then calls moveToStage(); on failure (a stage-transition
 * rule blocks it, e.g. moving into an "Offer" stage without a completed interview) the card is
 * moved back to its original column and the server's error message is shown — see onDrop().
 */
@Component({
  selector: 'vespera-candidate-pipeline',
  imports: [
    ReactiveFormsModule,
    CdkDropListGroup,
    CdkDropList,
    CdkDrag,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    FormErrorComponent,
    ErrorStateComponent,
    LoadingStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './candidate-pipeline.component.html',
})
export class CandidatePipelineComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly recruitmentFacade = inject(RecruitmentFacade);

  readonly loading = this.recruitmentFacade.pipelineLoading;
  readonly error = this.recruitmentFacade.pipelineError;
  readonly statusLabel = (status: CandidateCardDto['status']): string =>
    status !== undefined ? CANDIDATE_STATUS_LABELS[status] : '';

  readonly columns = signal<PipelineColumn[]>([]);
  readonly dropError = signal<string | null>(null);

  readonly candidateForm = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required]],
  });
  readonly addingCandidate = signal(false);
  readonly addCandidateError = signal<string | null>(null);

  private requisitionId = '';

  constructor() {
    // `columns` is local, CDK-mutable state (a re-derived computed signal wouldn't give
    // cdkDropList a stable array reference to transferArrayItem between) — this effect is what
    // keeps it in sync with the facade's pipeline signal, which only resolves after the async
    // HTTP response arrives; reading the facade signal synchronously right after calling
    // loadPipeline() would see the stale pre-response value, so this must be reactive, not a
    // one-shot read-after-call.
    effect(
      () => {
        const pipeline = this.recruitmentFacade.pipeline();
        this.columns.set(this.buildColumns(pipeline?.stages ?? [], pipeline?.candidates ?? []));
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.recruitmentFacade.loadPipeline(this.requisitionId);
  }

  private buildColumns(stages: PipelineStageDto[], candidates: CandidateCardDto[]): PipelineColumn[] {
    const orderedStages = [...stages].sort((a, b) => (a.sequenceNumber ?? 0) - (b.sequenceNumber ?? 0));
    const unassigned: PipelineColumn = {
      id: UNASSIGNED_COLUMN_ID,
      name: 'Unassigned',
      candidates: candidates.filter((candidate) => !candidate.currentPipelineStageId),
    };

    const stageColumns: PipelineColumn[] = orderedStages.map((stage) => ({
      id: stage.id!,
      name: stage.name ?? '',
      candidates: candidates.filter((candidate) => candidate.currentPipelineStageId === stage.id),
    }));

    return [unassigned, ...stageColumns];
  }

  openCandidate(candidate: CandidateCardDto): void {
    if (candidate.id) {
      void this.router.navigate(['/recruitment/candidates', candidate.id]);
    }
  }

  onDrop(event: CdkDragDrop<CandidateCardDto[]>, targetColumnId: string): void {
    if (event.previousContainer === event.container) {
      return;
    }

    const candidate = event.previousContainer.data[event.previousIndex];
    const sourceContainerData = event.previousContainer.data;
    const sourceIndex = event.previousIndex;
    const targetContainerData = event.container.data;
    const targetIndex = event.currentIndex;

    // Optimistic move.
    transferArrayItem(sourceContainerData, targetContainerData, sourceIndex, targetIndex);
    this.dropError.set(null);

    if (targetColumnId === UNASSIGNED_COLUMN_ID || !candidate.id) {
      // Nothing to persist for a drop back into the synthetic "Unassigned" column — there's no
      // "un-move" endpoint, so just leave the optimistic move in place locally.
      return;
    }

    this.recruitmentFacade
      .moveToStage(candidate.id, { targetStageId: targetColumnId })
      .pipe(
        catchError((apiError: ApiError) => {
          // Revert: move the card back to its original position in its original column.
          transferArrayItem(
            targetContainerData,
            sourceContainerData,
            targetContainerData.indexOf(candidate),
            sourceIndex,
          );
          this.dropError.set(apiError.message);
          return of(null);
        }),
      )
      .subscribe();
  }

  addCandidate(): void {
    if (this.candidateForm.invalid || this.addingCandidate()) {
      this.candidateForm.markAllAsTouched();
      return;
    }

    this.addingCandidate.set(true);
    this.addCandidateError.set(null);

    this.recruitmentFacade
      .createCandidate({ jobRequisitionId: this.requisitionId, ...this.candidateForm.getRawValue() })
      .pipe(
        catchError((apiError: ApiError) => {
          this.addCandidateError.set(apiError.message);
          this.addingCandidate.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.addingCandidate.set(false);
        this.candidateForm.reset({ fullName: '', email: '', phone: '' });
        this.reload();
      });
  }
}
