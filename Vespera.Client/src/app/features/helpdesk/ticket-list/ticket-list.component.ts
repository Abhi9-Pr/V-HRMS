import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { TICKET_PRIORITY_LABELS, TICKET_STATUS_LABELS } from '../helpdesk.labels';
import { TicketCreateDialogComponent } from './ticket-create-dialog.component';
import { Permissions, TicketSummaryDto } from 'vespera-shared';

@Component({
  selector: 'vespera-ticket-list',
  standalone: true,
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule],
  templateUrl: './ticket-list.component.html',
})
export class TicketListComponent implements OnInit, AfterViewInit {
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: TicketSummaryDto }>;

  readonly Permissions = Permissions;

  readonly tickets = this.helpdeskFacade.tickets;
  readonly totalCount = this.helpdeskFacade.ticketsTotalCount;
  readonly loading = this.helpdeskFacade.ticketsLoading;
  readonly error = this.helpdeskFacade.ticketsError;

  columns: DataTableColumn<TicketSummaryDto>[] = [
    { key: 'subject', header: 'Subject', cell: (row) => row.subject ?? '' },
    {
      key: 'priority',
      header: 'Priority',
      cell: (row) => (row.priority !== undefined ? TICKET_PRIORITY_LABELS[row.priority] : ''),
    },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => (row.status !== undefined ? TICKET_STATUS_LABELS[row.status] : ''),
    },
    { key: 'dueAt', header: 'Due', cell: (row) => (row.dueAt ? new Date(row.dueAt).toLocaleString() : '—') },
    { key: 'assignedTo', header: 'Assigned to', cell: (row) => row.assignedTo ?? '—' },
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
    this.helpdeskFacade.loadTickets(this.lastQuery);
  }

  openCreateDialog(): void {
    this.dialog
      .open(TicketCreateDialogComponent)
      .afterClosed()
      .subscribe((created: boolean) => {
        if (created) {
          this.reload();
        }
      });
  }

  openTicket(ticket: TicketSummaryDto): void {
    if (!ticket.id) {
      return;
    }

    void this.router.navigate(['/helpdesk', ticket.id]);
  }
}
