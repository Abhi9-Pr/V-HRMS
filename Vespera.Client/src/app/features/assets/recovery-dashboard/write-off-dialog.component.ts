import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { AssetsFacade } from '../data/assets.facade';
import { CURRENCY_LABELS } from '../assets.labels';
import { ApiError, Currency } from 'vespera-shared';

export interface WriteOffDialogData {
  recoveryId: string;
}

@Component({
    selector: 'vespera-write-off-dialog',
    imports: [
        ReactiveFormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatButtonModule,
        FormErrorComponent,
    ],
    templateUrl: './write-off-dialog.component.html'
})
export class WriteOffDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialogRef = inject(MatDialogRef<WriteOffDialogComponent>);

  readonly data = inject<WriteOffDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];

  readonly form = this.formBuilder.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency._0, [Validators.required]],
    reason: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.assetsFacade
      .writeOffRecovery(this.data.recoveryId, this.form.getRawValue())
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
