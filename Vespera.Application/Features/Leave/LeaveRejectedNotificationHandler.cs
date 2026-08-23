using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave.Events;

namespace Vespera.Application.Features.Leave;

public sealed class LeaveRejectedNotificationHandler : INotificationHandler<DomainEventNotification<LeaveRejected>>
{
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly INotificationDispatcher _dispatcher;

    public LeaveRejectedNotificationHandler(IReadRepositoryAdmin<User> usersAdmin, INotificationDispatcher dispatcher)
    {
        _usersAdmin = usersAdmin;
        _dispatcher = dispatcher;
    }

    public async Task Handle(DomainEventNotification<LeaveRejected> notification, CancellationToken cancellationToken)
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
                "Leave request rejected",
                $"Your leave request was rejected: {domainEvent.Reason}",
                new Dictionary<string, string> { ["email"] = recipient.Email.Value }),
            cancellationToken);
    }
}
