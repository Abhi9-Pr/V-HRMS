import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { CURRENCY_LABELS, Currency } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { EXPENSE_CLAIM_STATUS_LABELS, ExpenseClaimDto } from '../expenses.models';

@Component({
  selector: 'app-approvals',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './approvals.html',
  styleUrl: './approvals.scss',
})
export class Approvals implements OnInit {
  protected readonly claims = signal<ExpenseClaimDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['status', 'settlementCurrency', 'total', 'actions'];

  private readonly fb = inject(FormBuilder);

  protected readonly commentControl = this.fb.nonNullable.control('');

  constructor(private readonly expenses: ExpensesService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.expenses.getPendingApprovals({ page: 1, pageSize: 50 }).subscribe((result) => {
      this.claims.set(result.items);
      this.loading.set(false);
    });
  }

  decide(claim: ExpenseClaimDto, approved: boolean): void {
    this.expenses
      .decideApproval(claim.id, { approved, comment: this.commentControl.value || null })
      .subscribe(() => this.load());
  }

  protected statusLabel(status: ExpenseClaimDto['status']): string {
    return EXPENSE_CLAIM_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }
}
