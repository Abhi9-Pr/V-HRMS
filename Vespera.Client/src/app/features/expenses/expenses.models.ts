import { Currency } from '../../core/models/currency';

// Mirrors Vespera.Domain.Expense.ExpenseClaimStatus exactly (ordinal values — see currency.ts).
export enum ExpenseClaimStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
  Reimbursed = 4,
}

export const EXPENSE_CLAIM_STATUS_LABELS: Record<ExpenseClaimStatus, string> = {
  [ExpenseClaimStatus.Draft]: 'Draft',
  [ExpenseClaimStatus.Submitted]: 'Submitted',
  [ExpenseClaimStatus.Approved]: 'Approved',
  [ExpenseClaimStatus.Rejected]: 'Rejected',
  [ExpenseClaimStatus.Reimbursed]: 'Reimbursed',
};

// Mirrors Vespera.Domain.Expense.ExpensePolicySeverity.
export enum ExpensePolicySeverity {
  Warn = 0,
  Block = 1,
}

export interface ExpenseLineDto {
  id: string;
  category: string;
  amount: number;
  currency: Currency;
  convertedAmount: number | null;
  exchangeRate: number | null;
  expenseDate: string;
  receiptReference: string | null;
  vendor: string | null;
  taxAmount: number | null;
}

export interface ExpenseClaimDto {
  id: string;
  status: ExpenseClaimStatus;
  settlementCurrency: Currency;
  total: number;
  lines: ExpenseLineDto[];
}

export interface PagedRequest {
  page?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortDescending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface OpenClaimRequest {
  settlementCurrency: Currency;
}

export interface ExpenseReceiptSuggestionsDto {
  vendor: string | null;
  expenseDate: string | null;
  taxAmount: number | null;
  amount: number | null;
  confidence: number;
}

export interface UploadExpenseReceiptResultDto {
  receiptReference: string;
  suggestions: ExpenseReceiptSuggestionsDto;
}

export interface AddLineRequest {
  category: string;
  amount: number;
  currency: Currency;
  expenseDate: string;
  receiptReference: string | null;
  vendor: string | null;
  taxAmount: number | null;
}

export interface SubmitExpenseClaimResultDto {
  warnings: string[];
}

export interface DecideApprovalRequest {
  approved: boolean;
  comment: string | null;
}

export interface SettleExpenseClaimRequest {
  expenseClaimId: string;
  payrollRunId: string;
}

export interface CreateExpensePolicyRequest {
  category: string;
  maxAmountPerClaim: number;
  receiptRequiredAboveAmount: number;
  currency: Currency;
  applicableDesignationId: string | null;
  maxAmountSeverity: ExpensePolicySeverity;
  receiptRequiredSeverity: ExpensePolicySeverity;
}

export interface ExpensePolicyDto {
  id: string;
  category: string;
  maxAmountPerClaim: number;
  receiptRequiredAboveAmount: number;
  currency: Currency;
  applicableDesignationId: string | null;
  maxAmountSeverity: ExpensePolicySeverity;
  receiptRequiredSeverity: ExpensePolicySeverity;
}
