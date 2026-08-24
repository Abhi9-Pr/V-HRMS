import { Currency } from '../../core/models/currency';
import { PagedRequest, PagedResult } from '../expenses/expenses.models';

// Mirrors Vespera.Domain.Assets.Asset.AssetStatus exactly (ordinal values — see currency.ts).
export enum AssetStatus {
  InStock = 0,
  Assigned = 1,
  UnderRepair = 2,
  Retired = 3,
}

export const ASSET_STATUS_LABELS: Record<AssetStatus, string> = {
  [AssetStatus.InStock]: 'In Stock',
  [AssetStatus.Assigned]: 'Assigned',
  [AssetStatus.UnderRepair]: 'Under Repair',
  [AssetStatus.Retired]: 'Retired',
};

// Mirrors Vespera.Domain.Assets.DepreciationSchedule.DepreciationMethod.
export enum DepreciationMethod {
  StraightLine = 0,
  DecliningBalance = 1,
}

export const DEPRECIATION_METHOD_LABELS: Record<DepreciationMethod, string> = {
  [DepreciationMethod.StraightLine]: 'Straight Line',
  [DepreciationMethod.DecliningBalance]: 'Declining Balance',
};

// Mirrors Vespera.Domain.Assets.AssetConditionReport.AssetConditionRating.
export enum AssetConditionRating {
  Excellent = 0,
  Good = 1,
  Fair = 2,
  Poor = 3,
  Damaged = 4,
}

export const ASSET_CONDITION_RATING_LABELS: Record<AssetConditionRating, string> = {
  [AssetConditionRating.Excellent]: 'Excellent',
  [AssetConditionRating.Good]: 'Good',
  [AssetConditionRating.Fair]: 'Fair',
  [AssetConditionRating.Poor]: 'Poor',
  [AssetConditionRating.Damaged]: 'Damaged',
};

// Mirrors Vespera.Domain.Assets.AssetRecovery.AssetRecoveryStatus.
export enum AssetRecoveryStatus {
  Pending = 0,
  InTransit = 1,
  Received = 2,
  DamageAssessed = 3,
  WrittenOff = 4,
  Completed = 5,
}

export const ASSET_RECOVERY_STATUS_LABELS: Record<AssetRecoveryStatus, string> = {
  [AssetRecoveryStatus.Pending]: 'Pending',
  [AssetRecoveryStatus.InTransit]: 'In Transit',
  [AssetRecoveryStatus.Received]: 'Received',
  [AssetRecoveryStatus.DamageAssessed]: 'Damage Assessed',
  [AssetRecoveryStatus.WrittenOff]: 'Written Off',
  [AssetRecoveryStatus.Completed]: 'Completed',
};

// Mirrors Vespera.Domain.Eis.EmployeeExitReason.
export enum EmployeeExitReason {
  Resignation = 0,
  Termination = 1,
  Retirement = 2,
  EndOfContract = 3,
}

export const EMPLOYEE_EXIT_REASON_LABELS: Record<EmployeeExitReason, string> = {
  [EmployeeExitReason.Resignation]: 'Resignation',
  [EmployeeExitReason.Termination]: 'Termination',
  [EmployeeExitReason.Retirement]: 'Retirement',
  [EmployeeExitReason.EndOfContract]: 'End of Contract',
};

export interface DepreciationScheduleDto {
  method: DepreciationMethod;
  usefulLifeMonths: number;
  salvageValue: number;
  salvageValueCurrency: Currency;
}

export interface AssetDto {
  id: string;
  assetTag: string;
  category: string;
  purchaseCost: number;
  purchaseCostCurrency: Currency;
  purchaseDate: string;
  serialNumber: string | null;
  macAddress: string | null;
  warrantyExpiryDate: string | null;
  status: AssetStatus;
  depreciation: DepreciationScheduleDto | null;
}

export interface AssetAssignmentSummaryDto {
  id: string;
  employeeId: string;
  assignedAt: string;
  returnedAt: string | null;
  returnCondition: string | null;
}

export interface AssetDetailDto extends AssetDto {
  bookValue: number | null;
  bookValueCurrency: Currency | null;
  assignments: AssetAssignmentSummaryDto[];
}

export interface CreateAssetRequest {
  assetTag: string;
  category: string;
  purchaseCost: number;
  purchaseCostCurrency: Currency;
  purchaseDate: string;
  serialNumber: string | null;
  macAddress: string | null;
  warrantyExpiryDate: string | null;
}

export interface ConfigureDepreciationRequest {
  method: DepreciationMethod;
  usefulLifeMonths: number;
  salvageValue: number;
  salvageValueCurrency: Currency;
}

export interface AssignAssetRequest {
  employeeId: string;
}

export interface RecordConditionRequest {
  rating: AssetConditionRating;
  notes: string | null;
}

export interface ReturnAssetRequest {
  condition: string;
}

export interface SoftwareLicenseDto {
  id: string;
  productName: string;
  seatCount: number;
  seatsUsed: number;
  expiresAt: string | null;
}

export interface CreateLicenseRequest {
  productName: string;
  seatCount: number;
  expiresAt: string | null;
}

export interface AllocateSeatRequest {
  employeeId: string;
}

export interface UnusedSeatsReportRowDto {
  licenseId: string;
  productName: string;
  seatCount: number;
  seatsUsed: number;
  unusedSeats: number;
  expiresAt: string | null;
}

export interface AssetRecoveryDto {
  id: string;
  assetAssignmentId: string;
  assetId: string;
  employeeId: string;
  initiatedAt: string;
  status: AssetRecoveryStatus;
  courierCarrier: string | null;
  courierTrackingReference: string | null;
  damageAssessmentNotes: string | null;
  writeOffAmount: number | null;
  writeOffAmountCurrency: Currency | null;
  writeOffReason: string | null;
}

export interface CourierDispatchRequest {
  carrier: string;
  trackingReference: string;
}

export interface DamageAssessmentRequest {
  notes: string;
}

export interface WriteOffRequest {
  amount: number;
  currency: Currency;
  reason: string;
}

export interface OffboardingChecklistItemDto {
  description: string;
  isComplete: boolean;
}

export interface OffboardingChecklistDto {
  id: string;
  employeeId: string;
  isComplete: boolean;
  items: OffboardingChecklistItemDto[];
}

export interface ExitEmployeeRequest {
  exitDate: string;
  reason: EmployeeExitReason;
}

export type { PagedRequest, PagedResult };
