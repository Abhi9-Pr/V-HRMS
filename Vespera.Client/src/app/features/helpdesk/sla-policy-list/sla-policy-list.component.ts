import { Component, OnInit, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { SlaPolicyDto } from '../../../core/api/generated/api-client';
import { Permissions } from '../../../core/authorization/permissions';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { SlaPolicyCreateDialogComponent } from './sla-policy-create-dialog.component';

@Component({
  selector: 'vespera-sla-policy-list',
  standalone: true,
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent],
  templateUrl: './sla-policy-list.component.html',
})
export class SlaPolicyListComponent implements OnInit {
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialog = inject(MatDialog);

  readonly Permissions = Permissions;

  readonly policies = this.helpdeskFacade.policies;
  readonly totalCount = this.helpdeskFacade.policiesTotalCount;
  readonly loading = this.helpdeskFacade.policiesLoading;
  readonly error = this.helpdeskFacade.policiesError;

  readonly columns: DataTableColumn<SlaPolicyDto>[] = [
    { key: 'name', header: 'Name', cell: (row) => row.name ?? '' },
    { key: 'responseTime', header: 'Response time', cell: (row) => row.responseTime ?? '' },
    { key: 'resolutionTime', header: 'Resolution time', cell: (row) => row.resolutionTime ?? '' },
    { key: 'businessHoursStart', header: 'Business hours', cell: (row) => `${row.businessHoursStart ?? ''} – ${row.businessHoursEnd ?? ''}` },
  ];

  private lastQuery: DataTableQuery = { page: 1, pageSize: 20, sortDescending: false };

  ngOnInit(): void {
    this.reload();
  }

  onQueryChange(query: DataTableQuery): void {
    this.lastQuery = query;
    this.reload();
  }

  reload(): void {
    this.helpdeskFacade.loadPolicies(this.lastQuery);
  }

  openCreateDialog(): void {
    this.dialog
      .open(SlaPolicyCreateDialogComponent)
      .afterClosed()
      .subscribe((created: boolean) => {
        if (created) {
          this.reload();
        }
      });
  }
}
