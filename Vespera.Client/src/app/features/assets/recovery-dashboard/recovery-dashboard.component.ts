import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { catchError, of } from 'rxjs';
import { PermissionButtonComponent } from '../../../shared/buttons/permission-button.component';
import { DataTableColumn, DataTableQuery } from '../../../shared/data-table/data-table.model';
import { DataTableComponent } from '../../../shared/data-table/data-table.component';
import { ErrorStateComponent } from '../../../shared/states/error-state.component';
import { AssetsFacade } from '../data/assets.facade';
import { ASSET_RECOVERY_STATUS_LABELS } from '../assets.labels';
import { CourierDispatchDialogComponent } from './courier-dispatch-dialog.component';
import { DamageAssessmentDialogComponent } from './damage-assessment-dialog.component';
import { WriteOffDialogComponent } from './write-off-dialog.component';
import { ApiError, AssetRecoveryDto, AssetRecoveryStatus, Permissions } from 'vespera-shared';

/**
 * Row actions are gated by AssetRecoveryStatus alone (not a permission difference) — every action
 * here is behind Assets.Recover, but only the transition(s) actually valid for that row's current
 * status render, mirroring Vespera.Domain/Assets/AssetRecovery.cs's own guard conditions exactly:
 * Pending -> courier dispatch or mark received (a hand-delivered return skips courier tracking);
 * InTransit -> mark received; Received -> damage assessment, write off, or complete;
 * DamageAssessed -> write off or complete; WrittenOff -> complete; Completed -> nothing (shouldn't
 * appear in the pending list at all, but no action renders regardless).
 */
@Component({
    selector: 'vespera-recovery-dashboard',
    imports: [DataTableComponent, PermissionButtonComponent, ErrorStateComponent],
    templateUrl: './recovery-dashboard.component.html'
})
export class RecoveryDashboardComponent implements OnInit, AfterViewInit {
  private readonly assetsFacade = inject(AssetsFacade);
  private readonly dialog = inject(MatDialog);

  @ViewChild('actionsCell', { static: true }) actionsCellTemplate!: TemplateRef<{ $implicit: AssetRecoveryDto }>;

  readonly Permissions = Permissions;
  readonly AssetRecoveryStatus = AssetRecoveryStatus;
  readonly statusLabel = (status: AssetRecoveryStatus): string => ASSET_RECOVERY_STATUS_LABELS[status];

  readonly recoveries = this.assetsFacade.pendingRecoveries;
  readonly totalCount = this.assetsFacade.pendingRecoveriesTotalCount;
  readonly loading = this.assetsFacade.pendingRecoveriesLoading;
  readonly error = this.assetsFacade.pendingRecoveriesError;

  readonly actionInFlight = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);

  columns: DataTableColumn<AssetRecoveryDto>[] = [
    { key: 'assetId', header: 'Asset', cell: (row) => row.assetId ?? '' },
    { key: 'employeeId', header: 'Employee', cell: (row) => row.employeeId ?? '' },
    { key: 'status', header: 'Status', cell: (row) => (row.status !== undefined ? this.statusLabel(row.status) : '') },
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
    this.assetsFacade.loadPendingRecoveries(this.lastQuery);
  }

  canDispatch(recovery: AssetRecoveryDto): boolean {
    return recovery.status === AssetRecoveryStatus._0;
  }

  canReceive(recovery: AssetRecoveryDto): boolean {
    return recovery.status === AssetRecoveryStatus._0 || recovery.status === AssetRecoveryStatus._1;
  }

  canAssessDamage(recovery: AssetRecoveryDto): boolean {
    return recovery.status === AssetRecoveryStatus._2;
  }

  canWriteOff(recovery: AssetRecoveryDto): boolean {
    return recovery.status === AssetRecoveryStatus._2 || recovery.status === AssetRecoveryStatus._3;
  }

  canComplete(recovery: AssetRecoveryDto): boolean {
    return (
      recovery.status === AssetRecoveryStatus._2 ||
      recovery.status === AssetRecoveryStatus._3 ||
      recovery.status === AssetRecoveryStatus._4
    );
  }

  openDispatchDialog(recovery: AssetRecoveryDto): void {
    if (!recovery.id) {
      return;
    }

    this.dialog
      .open(CourierDispatchDialogComponent, { data: { recoveryId: recovery.id } })
      .afterClosed()
      .subscribe((succeeded: boolean) => {
        if (succeeded) {
          this.reload();
        }
      });
  }

  openDamageAssessmentDialog(recovery: AssetRecoveryDto): void {
    if (!recovery.id) {
      return;
    }

    this.dialog
      .open(DamageAssessmentDialogComponent, { data: { recoveryId: recovery.id } })
      .afterClosed()
      .subscribe((succeeded: boolean) => {
        if (succeeded) {
          this.reload();
        }
      });
  }

  openWriteOffDialog(recovery: AssetRecoveryDto): void {
    if (!recovery.id) {
      return;
    }

    this.dialog
      .open(WriteOffDialogComponent, { data: { recoveryId: recovery.id } })
      .afterClosed()
      .subscribe((succeeded: boolean) => {
        if (succeeded) {
          this.reload();
        }
      });
  }

  markReceived(recovery: AssetRecoveryDto): void {
    if (!recovery.id) {
      return;
    }

    this.runAction(recovery.id, this.assetsFacade.recordReceived(recovery.id));
  }

  complete(recovery: AssetRecoveryDto): void {
    if (!recovery.id) {
      return;
    }

    this.runAction(recovery.id, this.assetsFacade.completeRecovery(recovery.id));
  }

  private runAction(recoveryId: string, action$: ReturnType<AssetsFacade['recordReceived']>): void {
    this.actionInFlight.set(recoveryId);
    this.actionError.set(null);

    action$
      .pipe(
        catchError((apiError: ApiError) => {
          this.actionError.set(apiError.message);
          this.actionInFlight.set(null);
          return of(null);
        }),
      )
      .subscribe((result) => {
        if (result === null) {
          return;
        }

        this.actionInFlight.set(null);
        this.reload();
      });
  }
}
