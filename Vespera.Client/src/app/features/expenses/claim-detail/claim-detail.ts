import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { EXPENSE_CLAIM_STATUS_LABELS, ExpenseClaimDto } from '../expenses.models';

@Component({
  selector: 'app-claim-detail',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './claim-detail.html',
  styleUrl: './claim-detail.scss',
})
export class ClaimDetail implements OnInit {
  protected readonly claim = signal<ExpenseClaimDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly uploadingReceipt = signal(false);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly receiptReference = signal<string | null>(null);

  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly lineColumns = ['category', 'amount', 'currency', 'vendor', 'expenseDate'];

  private readonly fb = inject(FormBuilder);

  protected readonly lineForm = this.fb.nonNullable.group({
    category: ['', Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currency: [Currency.Inr, Validators.required],
    expenseDate: [new Date(), Validators.required],
    vendor: [''],
    taxAmount: [null as number | null],
  });

  private claimId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly expenses: ExpensesService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.claimId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.expenses.getClaimById(this.claimId).subscribe((claim) => {
      this.claim.set(claim);
      this.loading.set(false);
    });
  }

  onReceiptSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }

    this.uploadingReceipt.set(true);
    this.expenses.uploadReceipt(this.claimId, file).subscribe((result) => {
      this.uploadingReceipt.set(false);
      this.receiptReference.set(result.receiptReference);

      const suggestions = result.suggestions;
      this.lineForm.patchValue({
        vendor: suggestions.vendor ?? this.lineForm.getRawValue().vendor,
        amount: suggestions.amount ?? this.lineForm.getRawValue().amount,
        taxAmount: suggestions.taxAmount ?? this.lineForm.getRawValue().taxAmount,
        expenseDate: suggestions.expenseDate ? new Date(suggestions.expenseDate) : this.lineForm.getRawValue().expenseDate,
      });

      this.snackBar.open(
        `Receipt scanned (confidence ${(suggestions.confidence * 100).toFixed(0)}%) — review the suggested values below.`,
        'Dismiss',
        { duration: 5000 },
      );
    });
  }

  addLine(): void {
    if (this.lineForm.invalid) {
      this.lineForm.markAllAsTouched();
      return;
    }

    const value = this.lineForm.getRawValue();
    this.expenses
      .addLine(this.claimId, {
        category: value.category,
        amount: value.amount,
        currency: value.currency,
        expenseDate: toDateOnly(value.expenseDate),
        receiptReference: this.receiptReference(),
        vendor: value.vendor || null,
        taxAmount: value.taxAmount,
      })
      .subscribe(() => {
        this.receiptReference.set(null);
        this.lineForm.reset({
          category: '',
          amount: 0,
          currency: Currency.Inr,
          expenseDate: new Date(),
          vendor: '',
          taxAmount: null,
        });
        this.load();
      });
  }

  submitClaim(): void {
    this.submitting.set(true);
    this.submitError.set(null);

    this.expenses.submitClaim(this.claimId).subscribe({
      next: (result) => {
        this.submitting.set(false);
        if (result.warnings.length > 0) {
          this.snackBar.open(result.warnings.join(' '), 'Dismiss', { duration: 8000 });
        }
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.submitError.set(err.error?.detail ?? 'Submission failed.');
      },
    });
  }

  protected statusLabel(status: ExpenseClaimDto['status']): string {
    return EXPENSE_CLAIM_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }
}

function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
