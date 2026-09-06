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
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { AssetsFacade } from '../data/assets.facade';
import { LicenseCreateDialogComponent } from './license-create-dialog.component';
import { AllocateSeatDialogComponent } from './allocate-seat-dialog.component';
import { ReleaseSeatDialogComponent } from './release-seat-dialog.component';
import { Permissions, SoftwareLicenseDto } from 'vespera-shared';

@Component({
  selector: 'vespera-license-list',
  imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule],
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: './license-list.component.html',
})
export class LicenseListComponent implements OnInit, AfterViewInit {
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialog = inject(MatDialog);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: SoftwareLicenseDto }>;

  readonly Permissions = Permissions;

  readonly licenses = this.assetsFacade.licenses;
  readonly totalCount = this.assetsFacade.licensesTotalCount;
  readonly loading = this.assetsFacade.licensesLoading;
  readonly error = this.assetsFacade.licensesError;

  columns: DataTableColumn<SoftwareLicenseDto>[] = [
    { key: 'productName', header: 'Product', cell: (row) => row.productName ?? '' },
    { key: 'seatCount', header: 'Seats', cell: (row) => `${row.seatCount ?? 0}` },
    { key: 'seatsUsed', header: 'Used', cell: (row) => `${row.seatsUsed ?? 0}` },
    {
      key: 'expiresAt',
      header: 'Expires',
      cell: (row) => (row.expiresAt ? new Date(row.expiresAt).toLocaleDateString() : '—'),
    },
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
    this.assetsFacade.loadLicenses(this.lastQuery);
  }

  openCreateDialog(): void {
    this.dialog
      .open(LicenseCreateDialogComponent)
      .afterClosed()
      .subscribe((created: boolean) => {
        if (created) {
          this.reload();
        }
      });
  }

  openAllocateDialog(license: SoftwareLicenseDto): void {
    if (!license.id) {
      return;
    }

    this.dialog
      .open(AllocateSeatDialogComponent, { data: { licenseId: license.id } })
      .afterClosed()
      .subscribe((allocated: boolean) => {
        if (allocated) {
          this.reload();
        }
      });
  }

  openReleaseDialog(): void {
    this.dialog
      .open(ReleaseSeatDialogComponent)
      .afterClosed()
      .subscribe((released: boolean) => {
        if (released) {
          this.reload();
        }
      });
  }
}
