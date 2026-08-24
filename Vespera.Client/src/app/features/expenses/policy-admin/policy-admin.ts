import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { ExpensePolicyDto, ExpensePolicySeverity } from '../expenses.models';

@Component({
  selector: 'app-policy-admin',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatTableModule],
  templateUrl: './policy-admin.html',
  styleUrl: './policy-admin.scss',
})
export class PolicyAdmin implements OnInit {
  protected readonly policies = signal<ExpensePolicyDto[]>([]);
  protected readonly displayedColumns = ['category', 'maxAmountPerClaim', 'receiptRequiredAboveAmount', 'currency'];
  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly severityOptions = [ExpensePolicySeverity.Warn, ExpensePolicySeverity.Block];

  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    category: ['', Validators.required],
    maxAmountPerClaim: [0, [Validators.required, Validators.min(0)]],
    receiptRequiredAboveAmount: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency.Inr, Validators.required],
    maxAmountSeverity: [ExpensePolicySeverity.Block, Validators.required],
    receiptRequiredSeverity: [ExpensePolicySeverity.Warn, Validators.required],
  });

  constructor(private readonly expenses: ExpensesService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.expenses.getPolicies({ page: 1, pageSize: 50 }).subscribe((result) => this.policies.set(result.items));
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.expenses
      .createPolicy({
        category: value.category,
        maxAmountPerClaim: value.maxAmountPerClaim,
        receiptRequiredAboveAmount: value.receiptRequiredAboveAmount,
        currency: value.currency,
        applicableDesignationId: null,
        maxAmountSeverity: value.maxAmountSeverity,
        receiptRequiredSeverity: value.receiptRequiredSeverity,
      })
      .subscribe(() => {
        this.form.reset({
          category: '',
          maxAmountPerClaim: 0,
          receiptRequiredAboveAmount: 0,
          currency: Currency.Inr,
          maxAmountSeverity: ExpensePolicySeverity.Block,
          receiptRequiredSeverity: ExpensePolicySeverity.Warn,
        });
        this.load();
      });
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }
}
