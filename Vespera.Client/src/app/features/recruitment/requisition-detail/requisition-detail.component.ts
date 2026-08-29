import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { LoadingStateComponent } from '../../../shared/states/loading-state.component';
import { RecruitmentFacade } from '../data/recruitment.facade';
import { JOB_REQUISITION_STATUS_LABELS, REQUISITION_APPROVAL_STATUS_LABELS } from '../recruitment.labels';
import {
  ApiError,
  JobRequisitionDto,
  JobRequisitionStatus,
  Permissions,
  RequisitionApprovalStatus,
} from 'vespera-shared';

/**
 * Action buttons are gated by RequisitionApprovalStatus (and, for Publish, also
 * JobRequisitionStatus + isPublished) alone — mirroring
 * Vespera.Domain/Recruitment/JobRequisition.cs's own guard conditions exactly:
 * Draft -> SubmitForApproval; PendingApproval -> Approve/Reject; Approved (+ Open + not yet
 * published) -> Publish. Same discipline as assets' recovery-dashboard status-gated actions.
 */
@Component({
  selector: 'vespera-requisition-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    FormErrorComponent,
    PermissionButtonComponent,
    ErrorStateComponent,
    LoadingStateComponent,
  ],
  templateUrl: './requisition-detail.component.html',
})
export class RequisitionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly recruitmentFacade = inject(RecruitmentFacade);

  readonly Permissions = Permissions;
  readonly RequisitionApprovalStatus = RequisitionApprovalStatus;
  readonly statusLabel = (status: JobRequisitionStatus): string => JOB_REQUISITION_STATUS_LABELS[status];
  readonly approvalStatusLabel = (status: RequisitionApprovalStatus): string =>
    REQUISITION_APPROVAL_STATUS_LABELS[status];

  readonly requisition = this.recruitmentFacade.requisition;
  readonly loading = this.recruitmentFacade.requisitionLoading;
  readonly error = this.recruitmentFacade.requisitionError;

  readonly stageForm = this.formBuilder.nonNullable.group({
    stageName: ['', [Validators.required, Validators.maxLength(200)]],
  });

  readonly decisionForm = this.formBuilder.nonNullable.group({
    comment: [''],
  });

  readonly addingStage = signal(false);
  readonly addStageError = signal<string | null>(null);

  readonly actionInFlight = signal(false);
  readonly actionError = signal<string | null>(null);

  private requisitionId = '';

  ngOnInit(): void {
    this.requisitionId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.recruitmentFacade.loadRequisitionById(this.requisitionId);
  }

  canSubmitForApproval(requisition: JobRequisitionDto): boolean {
    return requisition.approvalStatus === RequisitionApprovalStatus._0;
  }

  canDecide(requisition: JobRequisitionDto): boolean {
    return requisition.approvalStatus === RequisitionApprovalStatus._1;
  }

  canPublish(requisition: JobRequisitionDto): boolean {
    return (
      requisition.approvalStatus === RequisitionApprovalStatus._2 &&
      requisition.status === JobRequisitionStatus._0 &&
      !requisition.isPublished
    );
  }

  openPipeline(): void {
    void this.router.navigate(['/recruitment/requisitions', this.requisitionId, 'pipeline']);
  }

  addStage(): void {
    if (this.stageForm.invalid || this.addingStage()) {
      this.stageForm.markAllAsTouched();
      return;
    }

    this.addingStage.set(true);
    this.addStageError.set(null);

    this.recruitmentFacade
      .addStage(this.requisitionId, this.stageForm.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.addStageError.set(apiError.message);
          this.addingStage.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.addingStage.set(false);
        this.stageForm.reset({ stageName: '' });
        this.reload();
      });
  }

  submitForApproval(): void {
    this.runAction(this.recruitmentFacade.submitForApproval(this.requisitionId));
  }

  approve(): void {
    this.runAction(
      this.recruitmentFacade.decideApproval(this.requisitionId, {
        approved: true,
        comment: this.decisionForm.getRawValue().comment,
      }),
    );
  }

  reject(): void {
    this.runAction(
      this.recruitmentFacade.decideApproval(this.requisitionId, {
        approved: false,
        comment: this.decisionForm.getRawValue().comment,
      }),
    );
  }

  publish(): void {
    this.runAction(this.recruitmentFacade.publish(this.requisitionId));
  }

  private runAction(action$: ReturnType<RecruitmentFacade['publish']>): void {
    this.actionInFlight.set(true);
    this.actionError.set(null);

    action$
      .pipe(
        catchError((apiError: ApiError) => {
          this.actionError.set(apiError.message);
          this.actionInFlight.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.actionInFlight.set(false);
        this.decisionForm.reset({ comment: '' });
        this.reload();
      });
  }
}
