import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { AssetsFacade } from '../data/assets.facade';
import { CURRENCY_LABELS } from '../assets.labels';
import { ApiError, Currency } from 'vespera-shared';

@Component({
  selector: 'vespera-asset-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
  ],
  templateUrl: './asset-create-dialog.component.html',
})
export class AssetCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialogRef = inject(MatDialogRef<AssetCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];

  readonly form = this.formBuilder.nonNullable.group({
    assetTag: ['', [Validators.required, Validators.maxLength(64)]],
    category: ['', [Validators.required, Validators.maxLength(100)]],
    purchaseCost: [0, [Validators.required, Validators.min(0)]],
    purchaseCostCurrency: [Currency._0, [Validators.required]],
    purchaseDate: [new Date(), [Validators.required]],
    serialNumber: [''],
    macAddress: [''],
    warrantyExpiryDate: [null as Date | null],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const {
      assetTag,
      category,
      purchaseCost,
      purchaseCostCurrency,
      purchaseDate,
      serialNumber,
      macAddress,
      warrantyExpiryDate,
    } = this.form.getRawValue();

    this.assetsFacade
      .createAsset({
        assetTag,
        category,
        purchaseCost,
        purchaseCostCurrency,
        purchaseDate,
        serialNumber: serialNumber || undefined,
        macAddress: macAddress || undefined,
        warrantyExpiryDate: warrantyExpiryDate ?? undefined,
      })
      .pipe(
        map((result) => result.id!),
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((assetId) => {
        if (assetId) {
          this.dialogRef.close(assetId);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
