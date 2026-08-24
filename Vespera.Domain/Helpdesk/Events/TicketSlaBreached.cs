using Vespera.Domain.Common;

namespace Vespera.Domain.Helpdesk.Events;

/// <summary>Carries <see cref="TenantId"/> explicitly (unlike most domain events, which lean on
/// the ambient tenant context) because this event's consumer runs out-of-band, replayed by the
/// outbox dispatcher with no HTTP request in play — see
/// <c>TicketSlaBreachedNotificationHandler</c>.</summary>
public sealed record TicketSlaBreached(TicketId TicketId, TenantId TenantId, DateTimeOffset DueAt, DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);
