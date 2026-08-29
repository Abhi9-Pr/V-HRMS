import { Component, OnInit, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { HelpdeskFacade } from '../data/helpdesk.facade';
import { CategoryCreateDialogComponent } from './category-create-dialog.component';
import { Permissions, TicketCategoryDto } from 'vespera-shared';

@Component({
  selector: 'vespera-category-list',
  standalone: true,
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent],
  templateUrl: './category-list.component.html',
})
export class CategoryListComponent implements OnInit {
  private readonly helpdeskFacade = inject(HelpdeskFacade);
  private readonly dialog = inject(MatDialog);

  readonly Permissions = Permissions;

  readonly categories = this.helpdeskFacade.categories;
  readonly totalCount = this.helpdeskFacade.categoriesTotalCount;
  readonly loading = this.helpdeskFacade.categoriesLoading;
  readonly error = this.helpdeskFacade.categoriesError;

  readonly columns: DataTableColumn<TicketCategoryDto>[] = [
    { key: 'name', header: 'Name', cell: (row) => row.name ?? '' },
    { key: 'departmentId', header: 'Department', cell: (row) => row.departmentId ?? '' },
    { key: 'defaultSlaPolicyId', header: 'Default SLA policy', cell: (row) => row.defaultSlaPolicyId ?? '—' },
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
    this.helpdeskFacade.loadCategories(this.lastQuery);
  }

  openCreateDialog(): void {
    this.dialog
      .open(CategoryCreateDialogComponent)
      .afterClosed()
      .subscribe((created: boolean) => {
        if (created) {
          this.reload();
        }
      });
  }
}
