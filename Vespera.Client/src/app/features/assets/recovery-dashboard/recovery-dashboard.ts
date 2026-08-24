import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { CURRENCY_LABELS, CURRENCY_OPTIONS, Currency } from '../../../core/models/currency';
import { AssetsService } from '../assets.service';
import { ASSET_RECOVERY_STATUS_LABELS, AssetRecoveryDto, AssetRecoveryStatus } from '../assets.models';

type ActionPanel = 'dispatch' | 'damage' | 'write-off' | null;

@Component({
  selector: 'app-recovery-dashboard',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './recovery-dashboard.html',
  styleUrl: './recovery-dashboard.scss',
})
export class RecoveryDashboard implements OnInit {
  protected readonly AssetRecoveryStatus = AssetRecoveryStatus;

  protected readonly recoveries = signal<AssetRecoveryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageSize = signal(20);
  protected readonly pageIndex = signal(0);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['assetId', 'employeeId', 'status', 'actions'];

  protected readonly activeRecoveryId = signal<string | null>(null);
  protected readonly activePanel = signal<ActionPanel>(null);
  protected readonly currencyOptions = CURRENCY_OPTIONS;

  private readonly fb = inject(FormBuilder);

  protected readonly dispatchForm = this.fb.nonNullable.group({
    carrier: ['', Validators.required],
    trackingReference: ['', Validators.required],
  });

  protected readonly damageForm = this.fb.nonNullable.group({
    notes: ['', Validators.required],
  });

  protected readonly writeOffForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0)]],
    currency: [Currency.Inr, Validators.required],
    reason: ['', Validators.required],
  });

  constructor(private readonly assetsService: AssetsService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.assetsService.getPendingRecoveries({ page: this.pageIndex() + 1, pageSize: this.pageSize() }).subscribe((result) => {
      this.recoveries.set(result.items);
      this.totalCount.set(result.totalCount);
      this.loading.set(false);
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  // Mirrors the guard conditions in Vespera.Domain.Assets.AssetRecovery exactly.
  canDispatch(status: AssetRecoveryStatus): boolean {
    return status === AssetRecoveryStatus.Pending;
  }

  canReceive(status: AssetRecoveryStatus): boolean {
    return status === AssetRecoveryStatus.Pending || status === AssetRecoveryStatus.InTransit;
  }

  canAssessDamage(status: AssetRecoveryStatus): boolean {
    return status === AssetRecoveryStatus.Received;
  }

  canWriteOff(status: AssetRecoveryStatus): boolean {
    return status === AssetRecoveryStatus.Received || status === AssetRecoveryStatus.DamageAssessed;
  }

  canComplete(status: AssetRecoveryStatus): boolean {
    return (
      status === AssetRecoveryStatus.Received ||
      status === AssetRecoveryStatus.DamageAssessed ||
      status === AssetRecoveryStatus.WrittenOff
    );
  }

  openPanel(recoveryId: string, panel: ActionPanel): void {
    this.activeRecoveryId.set(recoveryId);
    this.activePanel.set(panel);
  }

  closePanel(): void {
    this.activeRecoveryId.set(null);
    this.activePanel.set(null);
  }

  receive(recoveryId: string): void {
    this.assetsService.recordReceived(recoveryId).subscribe(() => this.load());
  }

  complete(recoveryId: string): void {
    this.assetsService.completeRecovery(recoveryId).subscribe(() => this.load());
  }

  submitDispatch(): void {
    const recoveryId = this.activeRecoveryId();
    if (!recoveryId || this.dispatchForm.invalid) {
      this.dispatchForm.markAllAsTouched();
      return;
    }

    this.assetsService.recordCourierDispatch(recoveryId, this.dispatchForm.getRawValue()).subscribe(() => {
      this.closePanel();
      this.load();
    });
  }

  submitDamage(): void {
    const recoveryId = this.activeRecoveryId();
    if (!recoveryId || this.damageForm.invalid) {
      this.damageForm.markAllAsTouched();
      return;
    }

    this.assetsService.recordDamageAssessment(recoveryId, this.damageForm.getRawValue()).subscribe(() => {
      this.closePanel();
      this.load();
    });
  }

  submitWriteOff(): void {
    const recoveryId = this.activeRecoveryId();
    if (!recoveryId || this.writeOffForm.invalid) {
      this.writeOffForm.markAllAsTouched();
      return;
    }

    this.assetsService.writeOffAsset(recoveryId, this.writeOffForm.getRawValue()).subscribe(() => {
      this.closePanel();
      this.load();
    });
  }

  protected statusLabel(status: AssetRecoveryStatus): string {
    return ASSET_RECOVERY_STATUS_LABELS[status];
  }

  protected currencyLabel(currency: Currency): string {
    return CURRENCY_LABELS[currency];
  }
}
