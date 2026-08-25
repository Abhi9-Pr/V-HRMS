import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { DepartmentsFacade } from '../data/departments.facade';
import { DepartmentFormDialogData } from './department-form-dialog.model';

@Component({
  selector: 'vespera-department-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    FormErrorComponent,
  ],
  templateUrl: './department-form-dialog.component.html',
})
export class DepartmentFormDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly departmentsFacade = inject(DepartmentsFacade);
  private readonly dialogRef = inject(MatDialogRef<DepartmentFormDialogComponent>);

  readonly data = inject<DepartmentFormDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly availableParents = this.data.availableParents.filter((department) => department.id !== this.data.department?.id);

  readonly form = this.formBuilder.nonNullable.group({
    name: [this.data.department?.name ?? '', [Validators.required, Validators.maxLength(200)]],
    code: [
      { value: this.data.department?.code ?? '', disabled: this.data.mode === 'edit' },
      [Validators.required, Validators.maxLength(20)],
    ],
    parentDepartmentId: [this.data.department?.parentDepartmentId ?? null],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const { name, code, parentDepartmentId } = this.form.getRawValue();

    const request$ =
      this.data.mode === 'create'
        ? this.departmentsFacade.create({ name, code, parentDepartmentId: parentDepartmentId ?? undefined }).pipe(map(() => true))
        : this.departmentsFacade
            .update(this.data.department!.id!, { name, parentDepartmentId: parentDepartmentId ?? undefined })
            .pipe(map(() => true));

    request$
      .pipe(
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(false);
        }),
      )
      .subscribe((succeeded) => {
        if (succeeded) {
          this.dialogRef.close(true);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
