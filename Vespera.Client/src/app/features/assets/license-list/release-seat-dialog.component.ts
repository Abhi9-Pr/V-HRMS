import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { catchError, map, of } from 'rxjs';
import { ApiError } from '../../../core/http/api-error.model';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { AssetsFacade } from '../data/assets.facade';

/** No per-allocation data is exposed by SoftwareLicenseDto (only aggregate seat counts) — same
 * "no picker exists, take the raw id" precedent as expenses' settlement screen, this takes the
 * allocation id directly rather than inventing a richer lookup UI the backend can't back. */
@Component({
  selector: 'vespera-release-seat-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, FormErrorComponent],
  templateUrl: './release-seat-dialog.component.html',
})
export class ReleaseSeatDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialogRef = inject(MatDialogRef<ReleaseSeatDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    allocationId: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.assetsFacade
      .releaseSeat(this.form.getRawValue().allocationId)
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
