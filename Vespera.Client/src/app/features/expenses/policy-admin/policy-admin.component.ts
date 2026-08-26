import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { catchError, of } from 'rxjs';
import { Currency, ExpensePolicyDto, ExpensePolicySeverity } from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { FormErrorComponent } from '../../../shared/form/form-error.component';
import { ExpensesFacade } from '../data/expenses.facade';
import { CURRENCY_LABELS, EXPENSE_POLICY_SEVERITY_LABELS } from '../expenses.labels';

@Component({
  selector: 'vespera-policy-admin',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, DataTableComponent, ErrorStateComponent, FormErrorComponent],
  templateUrl: './policy-admin.component.html',
})
export class PolicyAdminComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly expensesFacade = inject(ExpensesFacade);

  readonly currencies = Object.values(Currency).filter((value): value is Currency => typeof value === 'number');
  readonly severities = Object.values(ExpensePolicySeverity).filter((value): value is ExpensePolicySeverity => typeof value === 'number');
  readonly currencyLabel = (currency: Currency): string => CURRENCY_LABELS[currency];
  readonly severityLabel = (severity: ExpensePolicySeverity): string => EXPENSE_POLICY_SEVERITY_LABELS[severity];

  readonly policies = this.expensesFacade.policies;
  readonly totalCount = this.expensesFacade.policiesTotalCount;
  readonly loading = this.expensesFacade.policiesLoading;
  readonly error = this.expensesFacade.policiesError;

  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);

  columns: DataTableColumn<ExpensePolicyDto>[] = [
    { key: 'category', header: 'Category', cell: (row) => row.category ?? '' },
    {
      key: 'maxAmountPerClaim',
      header: 'Max per claim',
      cell: (row) => (row.maxAmountPerClaim !== undefined && row.currency !== undefined ? `${row.maxAmountPerClaim} ${CURRENCY_LABELS[row.currency]}` : ''),
    },
    {
      key: 'receiptRequiredAboveAmount',
      header: 'Receipt required above',
      cell: (row) =>
        row.receiptRequiredAboveAmount !== undefined && row.currency !== undefined
          ? `${row.receiptRequiredAboveAmount} ${CURRENCY_LABELS[row.currency]}`
          : '',
    },
    { key: 'maxAmountSeverity', header: 'Cap severity', cell: (row) => (row.maxAmountSeverity !== undefined ? this.severityLabel(row.maxAmountSeverity) : '') },
    {
      key: 'receiptRequiredSeverity',
      header: 'Receipt severity',
      cell: (row) => (row.receiptRequiredSeverity !== undefined ? this.severityLabel(row.receiptRequiredSeverity) : ''),
    },
  ];

  private lastQuery: DataTableQuery = { page: 1, pageSize: 20, sortDescending: false };

  readonly form = this.formBuilder.nonNullable.group({
    category: ['', [Validators.required, Validators.maxLength(100)]],
    maxAmountPerClaim: [0, [Validators.required, Validators.min(0)]],
    receiptRequiredAboveAmount: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency._0, [Validators.required]],
    maxAmountSeverity: [ExpensePolicySeverity._1, [Validators.required]],
    receiptRequiredSeverity: [ExpensePolicySeverity._0, [Validators.required]],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.expensesFacade.loadPolicies(this.lastQuery);
  }

  onQueryChange(query: DataTableQuery): void {
    this.lastQuery = query;
    this.reload();
  }

  createPolicy(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);

    this.expensesFacade
      .createPolicy(this.form.getRawValue())
      .pipe(
        catchError((apiError: ApiError) => {
          this.saveError.set(apiError.message);
          this.saving.set(false);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.saving.set(false);
        this.form.reset({
          category: '',
          maxAmountPerClaim: 0,
          receiptRequiredAboveAmount: 0,
          currency: Currency._0,
          maxAmountSeverity: ExpensePolicySeverity._1,
          receiptRequiredSeverity: ExpensePolicySeverity._0,
        });
        this.reload();
      });
  }
}
