import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RecruitmentService } from '../recruitment.service';
import {
  JOB_REQUISITION_STATUS_LABELS,
  JobRequisitionDto,
  JobRequisitionStatus,
  REQUISITION_APPROVAL_STATUS_LABELS,
  RequisitionApprovalStatus,
} from '../recruitment.models';

@Component({
  selector: 'app-requisition-detail',
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule],
  templateUrl: './requisition-detail.html',
  styleUrl: './requisition-detail.scss',
})
export class RequisitionDetail implements OnInit {
  readonly requisition = signal<JobRequisitionDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly decisionFormOpen = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly fb = inject(FormBuilder);

  protected readonly stageForm = this.fb.nonNullable.group({
    stageName: ['', Validators.required],
  });

  protected readonly decisionForm = this.fb.nonNullable.group({
    comment: [''],
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
    this.recruitment.getRequisitionById(this.requisitionId).subscribe((requisition) => {
      this.requisition.set(requisition);
      this.loading.set(false);
    });
  }

  addStage(): void {
    if (this.stageForm.invalid) {
      this.stageForm.markAllAsTouched();
      return;
    }

    this.recruitment.addStage(this.requisitionId, this.stageForm.getRawValue()).subscribe(() => {
      this.stageForm.reset({ stageName: '' });
      this.load();
    });
  }

  // Mirrors the guard conditions in Vespera.Domain.Recruitment.JobRequisition exactly.
  canSubmitForApproval(requisition: JobRequisitionDto): boolean {
    return requisition.approvalStatus === RequisitionApprovalStatus.Draft;
  }

  canDecide(requisition: JobRequisitionDto): boolean {
    return requisition.approvalStatus === RequisitionApprovalStatus.PendingApproval;
  }

  canPublish(requisition: JobRequisitionDto): boolean {
    return (
      requisition.approvalStatus === RequisitionApprovalStatus.Approved &&
      requisition.status === JobRequisitionStatus.Open &&
      !requisition.isPublished
    );
  }

  submitForApproval(): void {
    this.recruitment.submitForApproval(this.requisitionId).subscribe(() => this.load());
  }

  openDecisionForm(): void {
    this.decisionFormOpen.set(true);
  }

  closeDecisionForm(): void {
    this.decisionFormOpen.set(false);
  }

  decide(approved: boolean): void {
    const comment = this.decisionForm.getRawValue().comment || null;
    this.errorMessage.set(null);
    this.recruitment.decideRequisitionApproval(this.requisitionId, { approved, comment }).subscribe({
      next: () => {
        this.decisionFormOpen.set(false);
        this.decisionForm.reset({ comment: '' });
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(err.error?.detail ?? 'Decision failed.');
      },
    });
  }

  publish(): void {
    this.errorMessage.set(null);
    this.recruitment.publish(this.requisitionId).subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(err.error?.detail ?? 'Publish failed.');
      },
    });
  }

  openPipeline(): void {
    void this.router.navigate(['/recruitment/requisitions', this.requisitionId, 'pipeline']);
  }

  protected statusLabel(status: JobRequisitionStatus): string {
    return JOB_REQUISITION_STATUS_LABELS[status];
  }

  protected approvalStatusLabel(status: RequisitionApprovalStatus): string {
    return REQUISITION_APPROVAL_STATUS_LABELS[status];
  }
}
