import { PagedRequest, PagedResult } from '../expenses/expenses.models';

// Mirrors Vespera.Domain.Helpdesk.Ticket.TicketStatus exactly (ordinal values — see currency.ts).
export enum TicketStatus {
  Open = 0,
  InProgress = 1,
  OnHold = 2,
  Resolved = 3,
  Closed = 4,
}

export const TICKET_STATUS_LABELS: Record<TicketStatus, string> = {
  [TicketStatus.Open]: 'Open',
  [TicketStatus.InProgress]: 'In Progress',
  [TicketStatus.OnHold]: 'On Hold',
  [TicketStatus.Resolved]: 'Resolved',
  [TicketStatus.Closed]: 'Closed',
};

// Mirrors Vespera.Domain.Helpdesk.Ticket.TicketPriority.
export enum TicketPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export const TICKET_PRIORITY_LABELS: Record<TicketPriority, string> = {
  [TicketPriority.Low]: 'Low',
  [TicketPriority.Medium]: 'Medium',
  [TicketPriority.High]: 'High',
  [TicketPriority.Critical]: 'Critical',
};

export const TICKET_PRIORITY_OPTIONS: TicketPriority[] = [
  TicketPriority.Low,
  TicketPriority.Medium,
  TicketPriority.High,
  TicketPriority.Critical,
];

export interface TicketCommentDto {
  id: string;
  authorId: string;
  body: string;
  isInternal: boolean;
  createdAt: string;
  parentCommentId: string | null;
  attachmentReferences: string[];
}

export interface TicketDto {
  id: string;
  subject: string;
  description: string;
  priority: TicketPriority;
  status: TicketStatus;
  categoryId: string;
  raisedBy: string;
  assignedTo: string | null;
  raisedAt: string;
  dueAt: string;
  resolvedAt: string | null;
  slaBreachNotified: boolean;
  slaWarningNotified: boolean;
  satisfactionRating: number | null;
  comments: TicketCommentDto[];
}

export interface TicketSummaryDto {
  id: string;
  subject: string;
  priority: TicketPriority;
  status: TicketStatus;
  categoryId: string;
  assignedTo: string | null;
  raisedAt: string;
  dueAt: string;
}

export interface RaiseTicketRequest {
  categoryId: string;
  subject: string;
  description: string;
  priority: TicketPriority;
}

export interface AddCommentRequest {
  body: string;
  isInternal: boolean;
  parentCommentId: string | null;
  attachmentReferences: string[] | null;
}

export interface AssignTicketRequest {
  employeeId: string;
}

export interface RateSatisfactionRequest {
  rating: number;
}

export interface TicketCategoryDto {
  id: string;
  name: string;
  departmentId: string;
  defaultSlaPolicyId: string | null;
}

export interface CreateTicketCategoryRequest {
  name: string;
  departmentId: string;
  defaultSlaPolicyId: string | null;
}

// SlaPolicyDto.responseTime/resolutionTime (System.TimeSpan) and *.businessHoursStart/End
// (System.TimeOnly) both serialize as plain "HH:mm:ss" strings — System.Text.Json's default
// converters for both types, confirmed against CreateSlaPolicyRequest below rather than assumed.
export interface SlaPolicyDto {
  id: string;
  name: string;
  responseTime: string;
  resolutionTime: string;
  businessHoursStart: string;
  businessHoursEnd: string;
}

export interface CreateSlaPolicyRequest {
  name: string;
  responseTimeHours: number;
  resolutionTimeHours: number;
  businessHoursStart: string;
  businessHoursEnd: string;
}

export interface PublicHolidayDto {
  id: string;
  date: string;
  name: string;
}

export interface CreatePublicHolidayRequest {
  date: string;
  name: string;
}

export interface CategoryComplianceRowDto {
  categoryId: string;
  categoryName: string;
  total: number;
  breached: number;
  compliancePercentage: number;
}

export interface SlaComplianceReportDto {
  totalResolvedOrClosed: number;
  breachedCount: number;
  onTimeCount: number;
  compliancePercentage: number;
  byCategory: CategoryComplianceRowDto[];
}

export type { PagedRequest, PagedResult };
