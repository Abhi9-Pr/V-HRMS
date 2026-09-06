import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { ApiError } from 'vespera-shared';

@Component({
    selector: 'vespera-category-create-dialog',
    imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
    templateUrl: './category-create-dialog.component.html'
})
export class CategoryCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialogRef = inject(MatDialogRef<CategoryCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    departmentId: ['', [Validators.required]],
    defaultSlaPolicyId: [''],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const { name, departmentId, defaultSlaPolicyId } = this.form.getRawValue();

    this.helpdeskFacade
      .createCategory({ name, departmentId, defaultSlaPolicyId: defaultSlaPolicyId || undefined })
      .pipe(
        map(() => true),
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
