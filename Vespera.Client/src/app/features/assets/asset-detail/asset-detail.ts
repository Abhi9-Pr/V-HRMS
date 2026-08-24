import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { AssetsService } from '../assets.service';
import {
  ASSET_STATUS_LABELS,
  AssetDetailDto,
  AssetStatus,
  DEPRECIATION_METHOD_LABELS,
  DepreciationMethod,
} from '../assets.models';

@Component({
  selector: 'app-asset-detail',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './asset-detail.html',
  styleUrl: './asset-detail.scss',
})
export class AssetDetail implements OnInit {
  protected readonly AssetStatus = AssetStatus;

  protected readonly asset = signal<AssetDetailDto | null>(null);
  protected readonly loading = signal(false);

  protected readonly currencyOptions = CURRENCY_OPTIONS;
  protected readonly depreciationMethodOptions = [DepreciationMethod.StraightLine, DepreciationMethod.DecliningBalance];
  protected readonly assignmentColumns = ['employeeId', 'assignedAt', 'returnedAt', 'returnCondition'];

  private readonly fb = inject(FormBuilder);

  protected readonly depreciationForm = this.fb.nonNullable.group({
    method: [DepreciationMethod.StraightLine, Validators.required],
    usefulLifeMonths: [36, [Validators.required, Validators.min(1)]],
    salvageValue: [0, [Validators.required, Validators.min(0)]],
    salvageValueCurrency: [Currency.Inr, Validators.required],
  });

  protected readonly assignForm = this.fb.nonNullable.group({
    employeeId: ['', Validators.required],
  });

  private assetId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly assetsService: AssetsService,
  ) {}

  ngOnInit(): void {
    this.assetId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.assetsService.getAssetById(this.assetId).subscribe((asset) => {
      this.asset.set(asset);
      this.loading.set(false);
    });
  }

  configureDepreciation(): void {
    if (this.depreciationForm.invalid) {
      this.depreciationForm.markAllAsTouched();
      return;
    }

    this.assetsService.configureDepreciation(this.assetId, this.depreciationForm.getRawValue()).subscribe(() => this.load());
  }

  assign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }

    this.assetsService.assignAsset(this.assetId, this.assignForm.getRawValue()).subscribe((result) => {
      this.assignForm.reset({ employeeId: '' });
      void this.router.navigate(['/assets/assignments', result.id]);
    });
  }

  markUnderRepair(): void {
    this.assetsService.markUnderRepair(this.assetId).subscribe(() => this.load());
  }

  retire(): void {
    this.assetsService.retireAsset(this.assetId).subscribe(() => this.load());
  }

  activeAssignmentId(): string | null {
    const active = this.asset()?.assignments.find((a) => a.returnedAt === null);
    return active ? active.id : null;
  }

  protected statusLabel(status: AssetStatus): string {
    return ASSET_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }

  protected depreciationMethodLabel(method: DepreciationMethod): string {
    return DEPRECIATION_METHOD_LABELS[method];
  }
}
