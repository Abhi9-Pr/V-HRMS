/**
 * TS shapes for the widgets whose payload is never returned by its own controller action —
 * `TodoItemDto`/`CorporateEventSummaryDto`/`AnnouncementSummaryDto` are reused straight from
 * `core/api/generated/api-client` instead (see each widget component), since NSwag already
 * generated those from a real endpoint. These five only ever travel inside
 * `DashboardWidgetEnvelopeDto.data` (typed `any` on the wire), so nothing generates them — they're
 * hand-kept in sync with their Application-layer DTO by name.
 */

export interface CelebrationSummaryDto {
  employeeId: string;
  employeeName: string;
  celebrationType: string;
  nextOccurrence: string;
  yearsCount: number;
}

export interface ShiftTrackerWidgetDto {
  employeeId: string | null;
  shiftName: string | null;
  shiftStart: string | null;
  shiftEnd: string | null;
  punchStatus: 'NotStarted' | 'In' | 'Out' | 'Unavailable';
  firstIn: string | null;
  lastOut: string | null;
  workedMinutes: number;
}

export interface LeaveBalanceWidgetRowDto {
  leaveTypeId: string;
  leaveTypeName: string;
  available: number;
}

export interface LeaveBalanceWidgetDto {
  balances: LeaveBalanceWidgetRowDto[];
}

export interface PendingApprovalsWidgetDto {
  totalCount: number;
  countBySubjectType: Record<string, number>;
}

export interface MyLatestPayslipDto {
  payslipId: string;
  generatedAt: string;
  netPay: number;
  currency: string;
  downloadUrl: string;
}
