import { Injectable, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AllocateSeatRequest,
  AllocateSeatResponse,
  AssetDetailDto,
  AssetDto,
  AssetOffboardingChecklistDto,
  AssetRecoveriesClient,
  AssetRecoveryDto,
  AssetsClient,
  AssignAssetRequest,
  AssignAssetResponse,
  ConfigureDepreciationRequest,
  CourierDispatchRequest,
  CreateAssetRequest,
  CreateAssetResponse,
  CreateLicenseRequest,
  CreateLicenseResponse,
  DamageAssessmentRequest,
  LicensesClient,
  OffboardingChecklistsClient,
  RecordConditionRequest,
  ReturnAssetRequest,
  SoftwareLicenseDto,
  UnusedSeatsReportRowDto,
  UploadSignatureResponse,
  WriteOffRequest,
} from '../../../core/api/generated/api-client';
import { ApiError } from '../../../core/http/api-error.model';
import { DataTableQuery } from '../../../shared/data-table/data-table.model';

/**
 * Wraps AssetsClient/LicensesClient/AssetRecoveriesClient/OffboardingChecklistsClient behind
 * signals + plain methods, same split as ExpensesFacade (see docs/CONTRIBUTING-frontend.md /
 * docs/frontend-state.md): `load*` methods manage their own loading/error signals and subscribe
 * internally, mutating methods delegate straight to the generated client and return the
 * Observable for the calling component to handle (see asset-detail.component.ts).
 */
@Injectable({ providedIn: 'root' })
export class AssetsFacade {
  private readonly assetsClient = inject(AssetsClient);
  private readonly licensesClient = inject(LicensesClient);
  private readonly recoveriesClient = inject(AssetRecoveriesClient);
  private readonly checklistsClient = inject(OffboardingChecklistsClient);

  private readonly assetsSignal = signal<AssetDto[]>([]);
  private readonly assetsTotalCountSignal = signal(0);
  private readonly assetsLoadingSignal = signal(false);
  private readonly assetsErrorSignal = signal<ApiError | null>(null);

  private readonly assetSignal = signal<AssetDetailDto | null>(null);
  private readonly assetLoadingSignal = signal(false);
  private readonly assetErrorSignal = signal<ApiError | null>(null);

  private readonly licensesSignal = signal<SoftwareLicenseDto[]>([]);
  private readonly licensesTotalCountSignal = signal(0);
  private readonly licensesLoadingSignal = signal(false);
  private readonly licensesErrorSignal = signal<ApiError | null>(null);

  private readonly unusedSeatsReportSignal = signal<UnusedSeatsReportRowDto[]>([]);
  private readonly unusedSeatsReportLoadingSignal = signal(false);
  private readonly unusedSeatsReportErrorSignal = signal<ApiError | null>(null);

  private readonly pendingRecoveriesSignal = signal<AssetRecoveryDto[]>([]);
  private readonly pendingRecoveriesTotalCountSignal = signal(0);
  private readonly pendingRecoveriesLoadingSignal = signal(false);
  private readonly pendingRecoveriesErrorSignal = signal<ApiError | null>(null);

  private readonly checklistSignal = signal<AssetOffboardingChecklistDto | null>(null);
  private readonly checklistLoadingSignal = signal(false);
  private readonly checklistErrorSignal = signal<ApiError | null>(null);

  readonly assets = this.assetsSignal.asReadonly();
  readonly assetsTotalCount = this.assetsTotalCountSignal.asReadonly();
  readonly assetsLoading = this.assetsLoadingSignal.asReadonly();
  readonly assetsError = this.assetsErrorSignal.asReadonly();

  readonly asset = this.assetSignal.asReadonly();
  readonly assetLoading = this.assetLoadingSignal.asReadonly();
  readonly assetError = this.assetErrorSignal.asReadonly();

  readonly licenses = this.licensesSignal.asReadonly();
  readonly licensesTotalCount = this.licensesTotalCountSignal.asReadonly();
  readonly licensesLoading = this.licensesLoadingSignal.asReadonly();
  readonly licensesError = this.licensesErrorSignal.asReadonly();

  readonly unusedSeatsReport = this.unusedSeatsReportSignal.asReadonly();
  readonly unusedSeatsReportLoading = this.unusedSeatsReportLoadingSignal.asReadonly();
  readonly unusedSeatsReportError = this.unusedSeatsReportErrorSignal.asReadonly();

  readonly pendingRecoveries = this.pendingRecoveriesSignal.asReadonly();
  readonly pendingRecoveriesTotalCount = this.pendingRecoveriesTotalCountSignal.asReadonly();
  readonly pendingRecoveriesLoading = this.pendingRecoveriesLoadingSignal.asReadonly();
  readonly pendingRecoveriesError = this.pendingRecoveriesErrorSignal.asReadonly();

  readonly checklist = this.checklistSignal.asReadonly();
  readonly checklistLoading = this.checklistLoadingSignal.asReadonly();
  readonly checklistError = this.checklistErrorSignal.asReadonly();

  loadAssets(query: DataTableQuery): void {
    this.assetsLoadingSignal.set(true);
    this.assetsErrorSignal.set(null);

    this.assetsClient.assets_GetAssets(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.assetsSignal.set(result.items ?? []);
        this.assetsTotalCountSignal.set(result.totalCount ?? 0);
        this.assetsLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.assetsErrorSignal.set(apiError);
        this.assetsLoadingSignal.set(false);
      },
    });
  }

  loadAssetById(id: string): void {
    this.assetLoadingSignal.set(true);
    this.assetErrorSignal.set(null);

    this.assetsClient.assets_GetAssetById(id).subscribe({
      next: (asset) => {
        this.assetSignal.set(asset);
        this.assetLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.assetErrorSignal.set(apiError);
        this.assetLoadingSignal.set(false);
      },
    });
  }

  loadLicenses(query: DataTableQuery): void {
    this.licensesLoadingSignal.set(true);
    this.licensesErrorSignal.set(null);

    this.licensesClient.licenses_GetLicenses(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.licensesSignal.set(result.items ?? []);
        this.licensesTotalCountSignal.set(result.totalCount ?? 0);
        this.licensesLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.licensesErrorSignal.set(apiError);
        this.licensesLoadingSignal.set(false);
      },
    });
  }

  loadUnusedSeatsReport(): void {
    this.unusedSeatsReportLoadingSignal.set(true);
    this.unusedSeatsReportErrorSignal.set(null);

    this.licensesClient.licenses_GetUnusedSeatsReport().subscribe({
      next: (rows) => {
        this.unusedSeatsReportSignal.set(rows ?? []);
        this.unusedSeatsReportLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.unusedSeatsReportErrorSignal.set(apiError);
        this.unusedSeatsReportLoadingSignal.set(false);
      },
    });
  }

  loadPendingRecoveries(query: DataTableQuery): void {
    this.pendingRecoveriesLoadingSignal.set(true);
    this.pendingRecoveriesErrorSignal.set(null);

    this.recoveriesClient.assetRecoveries_GetPending(query.page, query.pageSize, query.sortBy, query.sortDescending).subscribe({
      next: (result) => {
        this.pendingRecoveriesSignal.set(result.items ?? []);
        this.pendingRecoveriesTotalCountSignal.set(result.totalCount ?? 0);
        this.pendingRecoveriesLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.pendingRecoveriesErrorSignal.set(apiError);
        this.pendingRecoveriesLoadingSignal.set(false);
      },
    });
  }

  loadOffboardingChecklist(employeeId: string): void {
    this.checklistLoadingSignal.set(true);
    this.checklistErrorSignal.set(null);

    this.checklistsClient.offboardingChecklists_GetForEmployee(employeeId).subscribe({
      next: (checklist) => {
        this.checklistSignal.set(checklist);
        this.checklistLoadingSignal.set(false);
      },
      error: (apiError: ApiError) => {
        this.checklistErrorSignal.set(apiError);
        this.checklistLoadingSignal.set(false);
      },
    });
  }

  createAsset(request: CreateAssetRequest): Observable<CreateAssetResponse> {
    return this.assetsClient.assets_CreateAsset(crypto.randomUUID(), request);
  }

  configureDepreciation(id: string, request: ConfigureDepreciationRequest): Observable<void> {
    return this.assetsClient.assets_ConfigureDepreciation(id, crypto.randomUUID(), request);
  }

  assignAsset(id: string, request: AssignAssetRequest): Observable<AssignAssetResponse> {
    return this.assetsClient.assets_AssignAsset(id, crypto.randomUUID(), request);
  }

  uploadHandoverSignature(assignmentId: string, file: File): Observable<UploadSignatureResponse> {
    return this.assetsClient.assets_UploadHandoverSignature(assignmentId, crypto.randomUUID(), { data: file, fileName: file.name });
  }

  recordCondition(assignmentId: string, request: RecordConditionRequest): Observable<void> {
    return this.assetsClient.assets_RecordCondition(assignmentId, crypto.randomUUID(), request);
  }

  returnAsset(assignmentId: string, request: ReturnAssetRequest): Observable<void> {
    return this.assetsClient.assets_ReturnAsset(assignmentId, crypto.randomUUID(), request);
  }

  markUnderRepair(id: string): Observable<void> {
    return this.assetsClient.assets_MarkUnderRepair(id, crypto.randomUUID());
  }

  retireAsset(id: string): Observable<void> {
    return this.assetsClient.assets_Retire(id, crypto.randomUUID());
  }

  createLicense(request: CreateLicenseRequest): Observable<CreateLicenseResponse> {
    return this.licensesClient.licenses_CreateLicense(crypto.randomUUID(), request);
  }

  allocateSeat(licenseId: string, request: AllocateSeatRequest): Observable<AllocateSeatResponse> {
    return this.licensesClient.licenses_AllocateSeat(licenseId, crypto.randomUUID(), request);
  }

  releaseSeat(allocationId: string): Observable<void> {
    return this.licensesClient.licenses_ReleaseSeat(allocationId, crypto.randomUUID());
  }

  recordCourierDispatch(recoveryId: string, request: CourierDispatchRequest): Observable<void> {
    return this.recoveriesClient.assetRecoveries_RecordCourierDispatch(recoveryId, crypto.randomUUID(), request);
  }

  recordReceived(recoveryId: string): Observable<void> {
    return this.recoveriesClient.assetRecoveries_RecordReceived(recoveryId, crypto.randomUUID());
  }

  recordDamageAssessment(recoveryId: string, request: DamageAssessmentRequest): Observable<void> {
    return this.recoveriesClient.assetRecoveries_RecordDamageAssessment(recoveryId, crypto.randomUUID(), request);
  }

  writeOffRecovery(recoveryId: string, request: WriteOffRequest): Observable<void> {
    return this.recoveriesClient.assetRecoveries_WriteOff(recoveryId, crypto.randomUUID(), request);
  }

  completeRecovery(recoveryId: string): Observable<void> {
    return this.recoveriesClient.assetRecoveries_Complete(recoveryId, crypto.randomUUID());
  }

  completeChecklistItem(checklistId: string, itemIndex: number): Observable<void> {
    return this.checklistsClient.offboardingChecklists_CompleteItem(checklistId, itemIndex, crypto.randomUUID());
  }
}
