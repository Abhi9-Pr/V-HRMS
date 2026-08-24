import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { idempotencyHeaders } from '../../core/http/idempotency';
import {
  AllocateSeatRequest,
  AssetDetailDto,
  AssetDto,
  AssetRecoveryDto,
  AssignAssetRequest,
  ConfigureDepreciationRequest,
  CourierDispatchRequest,
  CreateAssetRequest,
  CreateLicenseRequest,
  DamageAssessmentRequest,
  ExitEmployeeRequest,
  OffboardingChecklistDto,
  PagedRequest,
  PagedResult,
  RecordConditionRequest,
  ReturnAssetRequest,
  SoftwareLicenseDto,
  UnusedSeatsReportRowDto,
  WriteOffRequest,
} from './assets.models';

@Injectable({ providedIn: 'root' })
export class AssetsService {
  private readonly assetsUrl = `${environment.apiBaseUrl}/assets`;
  private readonly licensesUrl = `${environment.apiBaseUrl}/licenses`;
  private readonly recoveriesUrl = `${environment.apiBaseUrl}/asset-recoveries`;
  private readonly checklistsUrl = `${environment.apiBaseUrl}/offboarding-checklists`;
  private readonly employeesUrl = `${environment.apiBaseUrl}/employees`;

  constructor(private readonly http: HttpClient) {}

  // Assets

  createAsset(request: CreateAssetRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.assetsUrl, request, { headers: idempotencyHeaders() });
  }

  configureDepreciation(assetId: string, request: ConfigureDepreciationRequest): Observable<void> {
    return this.http.post<void>(`${this.assetsUrl}/${assetId}/depreciation`, request, {
      headers: idempotencyHeaders(),
    });
  }

  assignAsset(assetId: string, request: AssignAssetRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.assetsUrl}/${assetId}/assign`, request, {
      headers: idempotencyHeaders(),
    });
  }

  uploadHandoverSignature(assignmentId: string, file: File): Observable<{ reference: string }> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ reference: string }>(`${this.assetsUrl}/assignments/${assignmentId}/signature`, formData, {
      headers: idempotencyHeaders(),
    });
  }

  recordCondition(assignmentId: string, request: RecordConditionRequest): Observable<void> {
    return this.http.post<void>(`${this.assetsUrl}/assignments/${assignmentId}/condition-reports`, request, {
      headers: idempotencyHeaders(),
    });
  }

  returnAsset(assignmentId: string, request: ReturnAssetRequest): Observable<void> {
    return this.http.post<void>(`${this.assetsUrl}/assignments/${assignmentId}/return`, request, {
      headers: idempotencyHeaders(),
    });
  }

  markUnderRepair(assetId: string): Observable<void> {
    return this.http.post<void>(`${this.assetsUrl}/${assetId}/under-repair`, {}, { headers: idempotencyHeaders() });
  }

  retireAsset(assetId: string): Observable<void> {
    return this.http.post<void>(`${this.assetsUrl}/${assetId}/retire`, {}, { headers: idempotencyHeaders() });
  }

  getAssets(paging: PagedRequest): Observable<PagedResult<AssetDto>> {
    return this.http.get<PagedResult<AssetDto>>(this.assetsUrl, { params: toHttpParams(paging) });
  }

  getAssetById(id: string): Observable<AssetDetailDto> {
    return this.http.get<AssetDetailDto>(`${this.assetsUrl}/${id}`);
  }

  // Licenses

  createLicense(request: CreateLicenseRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.licensesUrl, request, { headers: idempotencyHeaders() });
  }

  getLicenses(paging: PagedRequest): Observable<PagedResult<SoftwareLicenseDto>> {
    return this.http.get<PagedResult<SoftwareLicenseDto>>(this.licensesUrl, { params: toHttpParams(paging) });
  }

  allocateSeat(licenseId: string, request: AllocateSeatRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.licensesUrl}/${licenseId}/allocations`, request, {
      headers: idempotencyHeaders(),
    });
  }

  releaseSeat(allocationId: string): Observable<void> {
    return this.http.post<void>(`${this.licensesUrl}/allocations/${allocationId}/release`, {}, {
      headers: idempotencyHeaders(),
    });
  }

  getUnusedSeatsReport(): Observable<UnusedSeatsReportRowDto[]> {
    return this.http.get<UnusedSeatsReportRowDto[]>(`${this.licensesUrl}/unused-seats-report`);
  }

  // Asset recoveries

  getPendingRecoveries(paging: PagedRequest): Observable<PagedResult<AssetRecoveryDto>> {
    return this.http.get<PagedResult<AssetRecoveryDto>>(this.recoveriesUrl, { params: toHttpParams(paging) });
  }

  recordCourierDispatch(recoveryId: string, request: CourierDispatchRequest): Observable<void> {
    return this.http.post<void>(`${this.recoveriesUrl}/${recoveryId}/courier-dispatch`, request, {
      headers: idempotencyHeaders(),
    });
  }

  recordReceived(recoveryId: string): Observable<void> {
    return this.http.post<void>(`${this.recoveriesUrl}/${recoveryId}/received`, {}, { headers: idempotencyHeaders() });
  }

  recordDamageAssessment(recoveryId: string, request: DamageAssessmentRequest): Observable<void> {
    return this.http.post<void>(`${this.recoveriesUrl}/${recoveryId}/damage-assessment`, request, {
      headers: idempotencyHeaders(),
    });
  }

  writeOffAsset(recoveryId: string, request: WriteOffRequest): Observable<void> {
    return this.http.post<void>(`${this.recoveriesUrl}/${recoveryId}/write-off`, request, {
      headers: idempotencyHeaders(),
    });
  }

  completeRecovery(recoveryId: string): Observable<void> {
    return this.http.post<void>(`${this.recoveriesUrl}/${recoveryId}/complete`, {}, { headers: idempotencyHeaders() });
  }

  // Offboarding checklist

  getChecklistForEmployee(employeeId: string): Observable<OffboardingChecklistDto> {
    return this.http.get<OffboardingChecklistDto>(`${this.checklistsUrl}/employees/${employeeId}`);
  }

  completeChecklistItem(checklistId: string, itemIndex: number): Observable<void> {
    return this.http.post<void>(`${this.checklistsUrl}/${checklistId}/items/${itemIndex}/complete`, {}, {
      headers: idempotencyHeaders(),
    });
  }

  // Employee exit

  exitEmployee(employeeId: string, request: ExitEmployeeRequest): Observable<void> {
    return this.http.post<void>(`${this.employeesUrl}/${employeeId}/exit`, request, { headers: idempotencyHeaders() });
  }
}

function toHttpParams(paging: PagedRequest): HttpParams {
  let params = new HttpParams();
  if (paging.page !== undefined) {
    params = params.set('page', paging.page);
  }
  if (paging.pageSize !== undefined) {
    params = params.set('pageSize', paging.pageSize);
  }
  if (paging.sortBy) {
    params = params.set('sortBy', paging.sortBy);
  }
  if (paging.sortDescending !== undefined) {
    params = params.set('sortDescending', paging.sortDescending);
  }
  return params;
}
