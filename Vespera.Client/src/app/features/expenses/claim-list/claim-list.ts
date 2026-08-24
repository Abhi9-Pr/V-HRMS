import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Currency, CURRENCY_LABELS } from '../../../core/models/currency';
import { ExpensesService } from '../expenses.service';
import { EXPENSE_CLAIM_STATUS_LABELS, ExpenseClaimDto } from '../expenses.models';

@Component({
  selector: 'app-claim-list',
  imports: [MatButtonModule, MatPaginatorModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './claim-list.html',
  styleUrl: './claim-list.scss',
})
export class ClaimList implements OnInit {
  protected readonly claims = signal<ExpenseClaimDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['status', 'settlementCurrency', 'total', 'lineCount'];
  constructor(
    private readonly expenses: ExpensesService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.expenses
      .getMyClaims({ page: this.pageIndex() + 1, pageSize: this.pageSize() })
      .subscribe((result) => {
        this.claims.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  createClaim(): void {
    this.expenses.openClaim({ settlementCurrency: Currency.Inr }).subscribe((result) => {
      void this.router.navigate(['/expenses/claims', result.id]);
    });
  }

  openClaim(claim: ExpenseClaimDto): void {
    void this.router.navigate(['/expenses/claims', claim.id]);
  }

  protected statusLabel(status: ExpenseClaimDto['status']): string {
    return EXPENSE_CLAIM_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }
}
