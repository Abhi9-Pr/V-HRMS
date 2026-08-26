import { Currency, ExpenseClaimStatus, ExpensePolicySeverity } from '../../core/api/generated/api-client';

/**
 * The backend's Currency/ExpenseClaimStatus/ExpensePolicySeverity enums have no
 * JsonStringEnumConverter registered, so NSwag generates them as bare numeric enums (`_0`, `_1`,
 * ...) with no way to recover a display name from the generated file itself. These lookups are
 * keyed by the exact C# enum declaration order (Vespera.Domain.ValueObjects.Currency /
 * Vespera.Domain.Expense.ExpenseClaimStatus / ExpensePolicySeverity) — if the backend enum order
 * ever changes, these must change with it.
 */
export const CURRENCY_LABELS: Record<Currency, string> = {
  [Currency._0]: 'INR',
  [Currency._1]: 'USD',
  [Currency._2]: 'EUR',
  [Currency._3]: 'GBP',
  [Currency._4]: 'AED',
  [Currency._5]: 'SGD',
};

export const EXPENSE_CLAIM_STATUS_LABELS: Record<ExpenseClaimStatus, string> = {
  [ExpenseClaimStatus._0]: 'Draft',
  [ExpenseClaimStatus._1]: 'Submitted',
  [ExpenseClaimStatus._2]: 'Approved',
  [ExpenseClaimStatus._3]: 'Rejected',
  [ExpenseClaimStatus._4]: 'Reimbursed',
};

export const EXPENSE_POLICY_SEVERITY_LABELS: Record<ExpensePolicySeverity, string> = {
  [ExpensePolicySeverity._0]: 'Warn',
  [ExpensePolicySeverity._1]: 'Block',
};
