import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RecruitmentService } from '../recruitment.service';
import { CANDIDATE_STATUS_LABELS, CandidateCardDto, CandidatePipelineDto, CandidateStatus } from '../recruitment.models';

const UNASSIGNED_COLUMN_ID = 'unassigned';

@Component({
  selector: 'app-candidate-pipeline',
  imports: [ReactiveFormsModule, CdkDropList, CdkDrag, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './candidate-pipeline.html',
  styleUrl: './candidate-pipeline.scss',
})
export class CandidatePipeline implements OnInit {
  readonly pipeline = signal<CandidatePipelineDto | null>(null);
  protected readonly loading = signal(false);

  protected readonly columns = computed(() => [...(this.pipeline()?.stages ?? [])].sort((a, b) => a.sequenceNumber - b.sequenceNumber));

  protected readonly unassignedColumnId = UNASSIGNED_COLUMN_ID;

  // Every stage column can receive a drop from any other stage column, or from the unassigned
  // column — but nothing can be dropped back INTO unassigned (there's no API to unset a
  // candidate's stage), so it's a valid drag source but never a listed target.
  protected readonly connectedStageColumnIds = computed(() => this.columns().map((s) => s.id));

  private readonly fb = inject(FormBuilder);

  protected readonly candidateForm = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
  });

  private requisitionId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly recruitment: RecruitmentService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.recruitment.getPipeline(this.requisitionId).subscribe((pipeline) => {
      this.pipeline.set(pipeline);
      this.loading.set(false);
    });
  }

  candidatesForStage(stageId: string | null): CandidateCardDto[] {
    return (this.pipeline()?.candidates ?? []).filter((c) => c.currentPipelineStageId === stageId);
  }

  addCandidate(): void {
    if (this.candidateForm.invalid) {
      this.candidateForm.markAllAsTouched();
      return;
    }

    const value = this.candidateForm.getRawValue();
    this.recruitment.createCandidate({ jobRequisitionId: this.requisitionId, ...value }).subscribe(() => {
      this.candidateForm.reset({ fullName: '', email: '', phone: '' });
      this.load();
    });
  }

  openCandidate(candidate: CandidateCardDto): void {
    void this.router.navigate(['/recruitment/candidates', candidate.id]);
  }

  onDrop(event: CdkDragDrop<CandidateCardDto[]>, targetStageId: string): void {
    if (event.previousContainer === event.container) {
      return;
    }

    const candidate = event.previousContainer.data[event.previousIndex];
    if (!candidate) {
      return;
    }

    this.moveCandidate(candidate.id, targetStageId);
  }

  // The core move logic, split out from onDrop so it's testable without simulating a real CDK
  // drag gesture: optimistically move the card locally, then revert to the pre-move snapshot if
  // the server rejects the transition (e.g. the "no completed interview yet" stage rule).
  moveCandidate(candidateId: string, targetStageId: string): void {
    const previous = this.pipeline();
    if (!previous) {
      return;
    }

    const optimistic: CandidatePipelineDto = {
      ...previous,
      candidates: previous.candidates.map((c) => (c.id === candidateId ? { ...c, currentPipelineStageId: targetStageId } : c)),
    };
    this.pipeline.set(optimistic);

    this.recruitment.moveToStage(candidateId, { targetStageId }).subscribe({
      next: () => {},
      error: (err: HttpErrorResponse) => {
        this.pipeline.set(previous);
        this.snackBar.open(err.error?.detail ?? 'Unable to move candidate to that stage.', 'Dismiss', { duration: 6000 });
      },
    });
  }

  protected statusLabel(status: CandidateStatus): string {
    return CANDIDATE_STATUS_LABELS[status];
  }
}
