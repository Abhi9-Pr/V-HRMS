using Vespera.Domain.Common;

namespace Vespera.Domain.Helpdesk.Events;

public sealed record TicketSlaBreached(TicketId TicketId, DateTimeOffset DueAt, DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
