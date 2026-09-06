import { Component, OnInit, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { PublicHolidayCreateDialogComponent } from './public-holiday-create-dialog.component';
import { Permissions, PublicHolidayDto } from 'vespera-shared';

@Component({
    selector: 'vespera-public-holiday-list',
    imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent],
    templateUrl: './public-holiday-list.component.html'
})
export class PublicHolidayListComponent implements OnInit {
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialog = inject(MatDialog);

  readonly Permissions = Permissions;

  readonly holidays = this.helpdeskFacade.holidays;
  readonly totalCount = this.helpdeskFacade.holidaysTotalCount;
  readonly loading = this.helpdeskFacade.holidaysLoading;
  readonly error = this.helpdeskFacade.holidaysError;

  readonly columns: DataTableColumn<PublicHolidayDto>[] = [
    { key: 'name', header: 'Name', cell: (row) => row.name ?? '' },
    { key: 'date', header: 'Date', cell: (row) => (row.date ? new Date(row.date).toLocaleDateString() : '') },
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
    this.helpdeskFacade.loadHolidays(this.lastQuery);
  }

  openCreateDialog(): void {
    this.dialog
      .open(PublicHolidayCreateDialogComponent)
      .afterClosed()
      .subscribe((created: boolean) => {
        if (created) {
          this.reload();
        }
      });
  }
}
