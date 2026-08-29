import { AssetConditionRating, AssetRecoveryStatus, AssetStatus, Currency, DepreciationMethod } from 'vespera-shared';

/**
 * None of these backend enums have a JsonStringEnumConverter registered, so NSwag generates them
 * as bare numeric enums (`_0`, `_1`, ...) with no display name recoverable from the generated file
 * itself. These lookups are keyed by the exact C# enum declaration order (Vespera.Domain.Assets.*,
 * Vespera.Domain.ValueObjects.Currency) — if the backend enum order ever changes, these must
 * change with it. Duplicated locally rather than shared with expenses.labels.ts's identical
 * CURRENCY_LABELS, matching this codebase's established per-feature convention.
 */
export const CURRENCY_LABELS: Record<Currency, string> = {
  [Currency._0]: 'INR',
  [Currency._1]: 'USD',
  [Currency._2]: 'EUR',
  [Currency._3]: 'GBP',
  [Currency._4]: 'AED',
  [Currency._5]: 'SGD',
};

export const ASSET_STATUS_LABELS: Record<AssetStatus, string> = {
  [AssetStatus._0]: 'In stock',
  [AssetStatus._1]: 'Assigned',
  [AssetStatus._2]: 'Under repair',
  [AssetStatus._3]: 'Retired',
};

export const DEPRECIATION_METHOD_LABELS: Record<DepreciationMethod, string> = {
  [DepreciationMethod._0]: 'Straight-line',
  [DepreciationMethod._1]: 'Declining balance',
};

export const ASSET_CONDITION_RATING_LABELS: Record<AssetConditionRating, string> = {
  [AssetConditionRating._0]: 'Excellent',
  [AssetConditionRating._1]: 'Good',
  [AssetConditionRating._2]: 'Fair',
  [AssetConditionRating._3]: 'Poor',
  [AssetConditionRating._4]: 'Damaged',
};

export const ASSET_RECOVERY_STATUS_LABELS: Record<AssetRecoveryStatus, string> = {
  [AssetRecoveryStatus._0]: 'Pending',
  [AssetRecoveryStatus._1]: 'In transit',
  [AssetRecoveryStatus._2]: 'Received',
  [AssetRecoveryStatus._3]: 'Damage assessed',
  [AssetRecoveryStatus._4]: 'Written off',
  [AssetRecoveryStatus._5]: 'Completed',
};
