import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { ExpensesFacade } from '../data/expenses.facade';
import { CURRENCY_LABELS } from '../expenses.labels';
import { DecisionDialogComponent } from './decision-dialog.component';
import { ExpenseClaimDto, Permissions } from 'vespera-shared';

@Component({
  selector: 'vespera-approvals',
  standalone: true,
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent],
  templateUrl: './approvals.component.html',
})
export class ApprovalsComponent implements OnInit, AfterViewInit {
  private readonly expensesFacade = inject(ExpensesFacade);
  private readonly dialog = inject(MatDialog);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: ExpenseClaimDto }>;

  readonly Permissions = Permissions;

  readonly claims = this.expensesFacade.pendingApprovals;
  readonly totalCount = this.expensesFacade.pendingApprovalsTotalCount;
  readonly loading = this.expensesFacade.pendingApprovalsLoading;
  readonly error = this.expensesFacade.pendingApprovalsError;

  columns: DataTableColumn<ExpenseClaimDto>[] = [
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
    this.expensesFacade.loadPendingApprovals(this.lastQuery);
  }

  decide(claim: ExpenseClaimDto, approved: boolean): void {
    if (!claim.id) {
      return;
    }

    this.dialog
      .open(DecisionDialogComponent, { data: { claimId: claim.id, approved } })
      .afterClosed()
      .subscribe((decided) => {
        if (decided) {
          this.reload();
        }
      });
  }
}
