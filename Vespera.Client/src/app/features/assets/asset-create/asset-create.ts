import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { AssetsService } from '../assets.service';

@Component({
  selector: 'app-asset-create',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatSelectModule,
  ],
  templateUrl: './asset-create.html',
  styleUrl: './asset-create.scss',
})
export class AssetCreate {
  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly currencyLabel = (currency: Currency) => CURRENCY_LABELS[currency];

  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    assetTag: ['', Validators.required],
    category: ['', Validators.required],
    purchaseCost: [0, [Validators.required, Validators.min(0)]],
    purchaseCostCurrency: [Currency.Inr, Validators.required],
    purchaseDate: [new Date(), Validators.required],
    serialNumber: [''],
    macAddress: [''],
    warrantyExpiryDate: [null as Date | null],
  });

  constructor(
    private readonly assetsService: AssetsService,
    private readonly router: Router,
  ) {}

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.assetsService
      .createAsset({
        assetTag: value.assetTag,
        category: value.category,
        purchaseCost: value.purchaseCost,
        purchaseCostCurrency: value.purchaseCostCurrency,
        purchaseDate: toDateOnly(value.purchaseDate),
        serialNumber: value.serialNumber || null,
        macAddress: value.macAddress || null,
        warrantyExpiryDate: value.warrantyExpiryDate ? toDateOnly(value.warrantyExpiryDate) : null,
      })
      .subscribe((result) => {
        void this.router.navigate(['/assets', result.id]);
      });
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
