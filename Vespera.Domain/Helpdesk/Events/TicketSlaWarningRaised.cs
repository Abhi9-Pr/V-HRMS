using Vespera.Domain.Common;

namespace Vespera.Domain.Helpdesk.Events;

/// <summary>Carries <see cref="TenantId"/> for the same out-of-band-consumer reason as
/// <see cref="TicketSlaBreached"/> — see <c>TicketSlaWarningRaisedNotificationHandler</c>.</summary>
public sealed record TicketSlaWarningRaised(TicketId TicketId, TenantId TenantId, DateTimeOffset DueAt, DateTimeOffset OccurredOn)
    : DomainEvent(OccurredOn);
