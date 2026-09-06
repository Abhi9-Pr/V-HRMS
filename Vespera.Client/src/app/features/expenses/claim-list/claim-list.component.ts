import {
  AfterViewInit,
  Component,
  OnInit,
  TemplateRef,
  ViewChild,
  inject,
  ChangeDetectionStrategy,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { ExpensesFacade } from '../data/expenses.facade';
import { CURRENCY_LABELS, EXPENSE_CLAIM_STATUS_LABELS } from '../expenses.labels';
import { ClaimCreateDialogComponent } from './claim-create-dialog.component';
import { ExpenseClaimDto, Permissions } from 'vespera-shared';

@Component({
  selector: 'vespera-claim-list',
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './claim-list.component.html',
})
export class ClaimListComponent implements OnInit, AfterViewInit {
  private readonly expensesFacade = inject(ExpensesFacade);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: ExpenseClaimDto }>;

  readonly Permissions = Permissions;

  readonly claims = this.expensesFacade.claims;
  readonly totalCount = this.expensesFacade.claimsTotalCount;
  readonly loading = this.expensesFacade.claimsLoading;
  readonly error = this.expensesFacade.claimsError;

  columns: DataTableColumn<ExpenseClaimDto>[] = [
    {
      key: 'status',
      header: 'Status',
      cell: (row) => (row.status !== undefined ? EXPENSE_CLAIM_STATUS_LABELS[row.status] : ''),
    },
    {
      key: 'total',
      header: 'Total',
      cell: (row) =>
        row.total !== undefined && row.settlementCurrency !== undefined
          ? `${row.total} ${CURRENCY_LABELS[row.settlementCurrency]}`
          : '',
    },
    { key: 'lines', header: 'Lines', cell: (row) => `${row.lines?.length ?? 0}` },
    { key: 'actions', header: '', cell: () => '' },
  ];

  private lastQuery: DataTableQuery = { page: 1, pageSize: 20, sortDescending: false };

  ngOnInit(): void {
    this.reload();
  }

  ngAfterViewInit(): void {
    this.columns = this.columns.map((column) =>
      column.key === 'actions' ? { ...column, cellTemplate: this.actionsCellTemplate } : column,
    );
  }

  onQueryChange(query: DataTableQuery): void {
    this.lastQuery = query;
    this.reload();
  }

  reload(): void {
    this.expensesFacade.loadMyClaims(this.lastQuery);
  }

  openClaim(claim: ExpenseClaimDto): void {
    if (claim.id) {
      void this.router.navigate(['/expenses', claim.id]);
    }
  }

  openCreateDialog(): void {
    this.dialog
      .open(ClaimCreateDialogComponent)
      .afterClosed()
      .subscribe((claimId: string | null) => {
        if (claimId) {
          void this.router.navigate(['/expenses', claimId]);
        }
      });
  }
}
