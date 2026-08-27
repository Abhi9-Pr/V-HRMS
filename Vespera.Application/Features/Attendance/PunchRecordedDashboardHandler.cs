using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Attendance;

/// <summary>Pushes the shift-tracker widget's live punch state over SignalR whenever a punch is
/// recorded, regardless of channel (web/mobile/biometric) — same outbox-driven, no-ambient-tenant
/// shape as <c>ApprovalStepAssignedNotificationHandler</c>. A structured push, not a toast, so it
/// goes through <see cref="IDashboardRealtimePublisher"/> rather than
/// <see cref="INotificationDispatcher"/>.</summary>
public sealed class PunchRecordedDashboardHandler : INotificationHandler<DomainEventNotification<PunchRecorded>>
{
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly IDashboardRealtimePublisher _publisher;

    public PunchRecordedDashboardHandler(IReadRepositoryAdmin<User> usersAdmin, IDashboardRealtimePublisher publisher)
    {
        _usersAdmin = usersAdmin;
        _publisher = publisher;
    }

    public async Task Handle(DomainEventNotification<PunchRecorded> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var users = await _usersAdmin.ListIgnoringFiltersAsync(
            new UserByEmployeeIdSpecification(domainEvent.TenantId, domainEvent.EmployeeId), cancellationToken);
        var recipient = users.Count > 0 ? users[0] : null;
        if (recipient is null)
        {
            return;
        }

        var status = domainEvent.PunchType.ToString();
        await _publisher.PublishPunchStateChangedAsync(
            recipient.Id.Value,
            new PunchStateChangedPayload(domainEvent.EmployeeId.Value, status, status, domainEvent.PunchedAtUtc),
            cancellationToken);
    }
}
