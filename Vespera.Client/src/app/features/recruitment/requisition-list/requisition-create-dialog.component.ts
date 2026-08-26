import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { RecruitmentFacade } from '../data/recruitment.facade';

@Component({
  selector: 'vespera-requisition-create-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, FormErrorComponent],
  templateUrl: './requisition-create-dialog.component.html',
})
export class RequisitionCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly recruitmentFacade = inject(RecruitmentFacade);
  private readonly dialogRef = inject(MatDialogRef<RequisitionCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    departmentId: ['', [Validators.required]],
    openingsCount: [1, [Validators.required, Validators.min(1)]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.recruitmentFacade
      .createRequisition(this.form.getRawValue())
      .pipe(
        map((result) => result.id!),
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((requisitionId) => {
        if (requisitionId) {
          this.dialogRef.close(requisitionId);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
