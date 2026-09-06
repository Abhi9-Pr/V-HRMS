import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { catchError, map, of } from 'rxjs';
import { ExpensesFacade } from '../data/expenses.facade';
import { CURRENCY_LABELS } from '../expenses.labels';
import { ApiError, Currency } from 'vespera-shared';

@Component({
    selector: 'vespera-claim-create-dialog',
    imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatSelectModule, MatButtonModule],
    templateUrl: './claim-create-dialog.component.html'
})
export class ClaimCreateDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly expensesFacade = inject(ExpensesFacade);
  private readonly dialogRef = inject(MatDialogRef<ClaimCreateDialogComponent>);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];

  readonly form = this.formBuilder.nonNullable.group({
    settlementCurrency: [Currency._0, [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.expensesFacade
      .openClaim({ settlementCurrency: this.form.getRawValue().settlementCurrency })
      .pipe(
        map((result) => result.id!),
        catchError((apiError: ApiError) => {
          this.error.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((claimId) => {
        if (claimId) {
          this.dialogRef.close(claimId);
        }
      });
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
