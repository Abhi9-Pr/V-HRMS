import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { AssetsService } from '../assets.service';
import { ASSET_STATUS_LABELS, AssetDto } from '../assets.models';

@Component({
  selector: 'app-asset-list',
  imports: [MatButtonModule, MatPaginatorModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.scss',
})
export class AssetList implements OnInit {
  protected readonly assets = signal<AssetDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['assetTag', 'category', 'status', 'serialNumber'];

  constructor(
    private readonly assetsService: AssetsService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.assetsService.getAssets({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.assets.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  newAsset(): void {
    void this.router.navigate(['/assets/new']);
  }

  openAsset(asset: AssetDto): void {
    void this.router.navigate(['/assets', asset.id]);
  }

  protected statusLabel(status: AssetStatusInput): string {
    return ASSET_STATUS_LABELS[status];
  }
}

type AssetStatusInput = AssetDto['status'];
