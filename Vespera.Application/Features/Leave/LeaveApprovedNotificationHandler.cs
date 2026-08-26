using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave.Events;

namespace Vespera.Application.Features.Leave;

/// <summary>Notifies the requesting employee once their leave request clears every tier. Same
/// no-ambient-tenant, <see cref="IReadRepositoryAdmin{T}"/> pattern as
/// <c>RegularizationApprovedDomainEventHandler</c> — this is why <see cref="LeaveApproved"/> now
/// carries <see cref="LeaveApproved.TenantId"/>.</summary>
public sealed class LeaveApprovedNotificationHandler : INotificationHandler<DomainEventNotification<LeaveApproved>>
{
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly INotificationDispatcher _dispatcher;

    public LeaveApprovedNotificationHandler(IReadRepositoryAdmin<User> usersAdmin, INotificationDispatcher dispatcher)
    {
        _usersAdmin = usersAdmin;
        _dispatcher = dispatcher;
    }

    public async Task Handle(DomainEventNotification<LeaveApproved> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var users = await _usersAdmin.ListIgnoringFiltersAsync(
            new UserByEmployeeIdSpecification(domainEvent.TenantId, domainEvent.EmployeeId), cancellationToken);
        if (users.Count == 0)
        {
            return;
        }

        var recipient = users[0];
        await _dispatcher.DispatchAsync(
            new NotificationMessage(
                recipient.Id.Value.ToString(),
                "Leave request approved",
                $"Your leave request for {domainEvent.Period.Start:yyyy-MM-dd} to {domainEvent.Period.End:yyyy-MM-dd} has been approved.",
                new Dictionary<string, string> { ["email"] = recipient.Email.Value }),
            cancellationToken);
    }
}
