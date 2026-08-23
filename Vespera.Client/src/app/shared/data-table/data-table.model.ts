import { TemplateRef } from '@angular/core';

export interface DataTableColumn<T> {
  key: string;
  header: string;
  /** Ignored when `cellTemplate` is set — required either way so a column always has a plain-text
   * fallback (e.g. for a future CSV/export path that can't render a template). */
  cell: (row: T) => string;
  /** For a column that needs more than text — action buttons, a status chip, etc. — a template
   * with `let-row` context. See department-list.component.html's actions column for the pattern. */
  cellTemplate?: TemplateRef<{ $implicit: T }>;
  sortable?: boolean;
}

/** Mirrors Vespera.Application.Common.PagedRequest exactly — the data table's output plugs
 * straight into a facade's `load()` call with no re-shaping. */
export interface DataTableQuery {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending: boolean;
}
