import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { AssetsFacade } from '../data/assets.facade';

export interface CourierDispatchDialogData {
  recoveryId: string;
}

@Component({
  selector: 'vespera-courier-dispatch-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, FormErrorComponent],
  templateUrl: './courier-dispatch-dialog.component.html',
})
export class CourierDispatchDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialogRef = inject(MatDialogRef<CourierDispatchDialogComponent>);

  readonly data = inject<CourierDispatchDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    carrier: ['', [Validators.required]],
    trackingReference: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.assetsFacade
      .recordCourierDispatch(this.data.recoveryId, this.form.getRawValue())
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
