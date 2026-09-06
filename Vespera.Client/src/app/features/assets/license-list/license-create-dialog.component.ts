import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { catchError, map, of } from 'rxjs';
import { AssetsFacade } from '../data/assets.facade';
import { ApiError } from 'vespera-shared';

@Component({
  selector: 'vespera-license-create-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './license-create-dialog.component.html',
})
export class LicenseCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialogRef = inject(MatDialogRef<LicenseCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    productName: ['', [Validators.required, Validators.maxLength(256)]],
    seatCount: [1, [Validators.required, Validators.min(1)]],
    expiresAt: [null as Date | null],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const { productName, seatCount, expiresAt } = this.form.getRawValue();

    this.assetsFacade
      .createLicense({ productName, seatCount, expiresAt: expiresAt ?? undefined })
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
