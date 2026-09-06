import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { catchError, of } from 'rxjs';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { ConfirmDialogComponent } from '../../../shared/dialogs/confirm-dialog.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { DepartmentsFacade } from '../data/departments.facade';
import { DepartmentFormDialogComponent } from '../department-form/department-form-dialog.component';
import { ApiError, DepartmentDto, Permissions } from 'vespera-shared';

import { PageHeaderComponent } from '../../../shared/page-header/page-header.component';

@Component({
    selector: 'vespera-department-list',
    imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule, PageHeaderComponent],
    templateUrl: './department-list.component.html'
})
export class DepartmentListComponent implements OnInit, AfterViewInit {
  private readonly departmentsFacade = inject(DepartmentsFacade);
  private readonly dialog = inject(MatDialog);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: DepartmentDto }>;

  readonly Permissions = Permissions;

  readonly departments = this.departmentsFacade.departments;
  readonly totalCount = this.departmentsFacade.totalCount;
  readonly loading = this.departmentsFacade.loading;
  readonly error = this.departmentsFacade.error;

  columns: DataTableColumn<DepartmentDto>[] = [
    { key: 'name', header: 'Name', cell: (row) => row.name ?? '', sortable: true },
    { key: 'code', header: 'Code', cell: (row) => row.code ?? '' },
    { key: 'actions', header: '', cell: () => '' },
  ];

  private lastQuery: DataTableQuery = { page: 1, pageSize: 20, sortDescending: false };
  readonly removeError = signal<string | null>(null);

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
    this.departmentsFacade.load(this.lastQuery);
  }

  openCreateDialog(): void {
    this.openFormDialog({ mode: 'create', availableParents: this.departments() });
  }

  openEditDialog(department: DepartmentDto): void {
    this.openFormDialog({ mode: 'edit', department, availableParents: this.departments() });
  }

  private openFormDialog(data: {
    mode: 'create' | 'edit';
    department?: DepartmentDto;
    availableParents: DepartmentDto[];
  }): void {
    this.dialog
      .open(DepartmentFormDialogComponent, { width: '480px', data })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.reload();
        }
      });
  }

  confirmDelete(department: DepartmentDto): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete department',
          message: `Delete "${department.name}"? This cannot be undone.`,
          danger: true,
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed && department.id) {
          this.removeError.set(null);
          this.departmentsFacade
            .remove(department.id)
            .pipe(
              catchError((apiError: ApiError) => {
                this.removeError.set(apiError.message);
                return of(null);
              }),
            )
            .subscribe((result) => {
              if (result !== null) {
                this.reload();
              }
            });
        }
      });
  }
}
