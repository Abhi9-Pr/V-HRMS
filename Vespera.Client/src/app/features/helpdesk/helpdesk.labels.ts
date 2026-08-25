import { TicketPriority, TicketStatus } from '../../core/api/generated/api-client';

/**
 * Neither backend enum has a JsonStringEnumConverter registered, so NSwag generates them as bare
 * numeric enums (`_0`, `_1`, ...) with no display name recoverable from the generated file itself.
 * Keyed by the exact C# enum declaration order (Vespera.Domain.Helpdesk.Ticket.TicketPriority /
 * TicketStatus) — if the backend enum order ever changes, these must change with it.
 */
export const TICKET_PRIORITY_LABELS: Record<TicketPriority, string> = {
  [TicketPriority._0]: 'Low',
  [TicketPriority._1]: 'Medium',
  [TicketPriority._2]: 'High',
  [TicketPriority._3]: 'Critical',
};

export const TICKET_STATUS_LABELS: Record<TicketStatus, string> = {
  [TicketStatus._0]: 'Open',
  [TicketStatus._1]: 'In progress',
  [TicketStatus._2]: 'On hold',
  [TicketStatus._3]: 'Resolved',
  [TicketStatus._4]: 'Closed',
};
