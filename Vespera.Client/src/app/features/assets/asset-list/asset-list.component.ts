import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { AssetsFacade } from '../data/assets.facade';
import { ASSET_STATUS_LABELS } from '../assets.labels';
import { AssetCreateDialogComponent } from './asset-create-dialog.component';
import { AssetDto, Permissions } from 'vespera-shared';

@Component({
    selector: 'vespera-asset-list',
    imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent, MatIconModule],
    templateUrl: './asset-list.component.html'
})
export class AssetListComponent implements OnInit, AfterViewInit {
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: AssetDto }>;

  readonly Permissions = Permissions;

  readonly assets = this.assetsFacade.assets;
  readonly totalCount = this.assetsFacade.assetsTotalCount;
  readonly loading = this.assetsFacade.assetsLoading;
  readonly error = this.assetsFacade.assetsError;

  columns: DataTableColumn<AssetDto>[] = [
    { key: 'assetTag', header: 'Asset tag', cell: (row) => row.assetTag ?? '' },
    { key: 'category', header: 'Category', cell: (row) => row.category ?? '' },
    {
      key: 'status',
      header: 'Status',
      cell: (row) => (row.status !== undefined ? ASSET_STATUS_LABELS[row.status] : ''),
    },
    { key: 'serialNumber', header: 'Serial number', cell: (row) => row.serialNumber ?? '—' },
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
    this.assetsFacade.loadAssets(this.lastQuery);
  }

  openAsset(asset: AssetDto): void {
    if (asset.id) {
      void this.router.navigate(['/assets', asset.id]);
    }
  }

  openCreateDialog(): void {
    this.dialog
      .open(AssetCreateDialogComponent)
      .afterClosed()
      .subscribe((assetId: string | null) => {
        if (assetId) {
          void this.router.navigate(['/assets', assetId]);
        }
      });
  }
}
