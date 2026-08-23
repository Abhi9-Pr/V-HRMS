import { Component, Input, OnChanges, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { EmptyStateComponent } from '../states/empty-state.component';
import { DataTableColumn, DataTableQuery } from './data-table.model';

/**
 * Server-side paging/sorting data table — every page/sort change re-emits the full
 * DataTableQuery (never filters/sorts client-side), because the row set on screen is always just
 * one page of a much larger server-side result. Filtering is left to the caller (a search box
 * feeding back into the same query) rather than baked in here, since what's filterable is
 * entirely feature-specific.
 */
@Component({
  selector: 'vespera-data-table',
  standalone: true,
  imports: [MatTableModule, MatSortModule, MatPaginatorModule, MatProgressBarModule, EmptyStateComponent, NgTemplateOutlet],
  templateUrl: './data-table.component.html',
})
export class DataTableComponent<T> implements OnChanges {
  @Input({ required: true }) columns: DataTableColumn<T>[] = [];
  @Input() rows: T[] = [];
  @Input() totalCount = 0;
  @Input() loading = false;
  @Input() pageSize = 20;
  @Input() pageSizeOptions = [10, 20, 50, 100];
  @Input() emptyLabel = 'No records found.';

  readonly queryChange = output<DataTableQuery>();

  displayedColumns: string[] = [];
  private currentPage = 1;
  private currentSort: Sort = { active: '', direction: '' };

  ngOnChanges(): void {
    this.displayedColumns = this.columns.map((column) => column.key);
  }

  onPage(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.emitQuery();
  }

  onSort(sort: Sort): void {
    this.currentSort = sort;
    this.currentPage = 1;
    this.emitQuery();
  }

  private emitQuery(): void {
    this.queryChange.emit({
      page: this.currentPage,
      pageSize: this.pageSize,
      sortBy: this.currentSort.direction ? this.currentSort.active : undefined,
      sortDescending: this.currentSort.direction === 'desc',
    });
  }
}
