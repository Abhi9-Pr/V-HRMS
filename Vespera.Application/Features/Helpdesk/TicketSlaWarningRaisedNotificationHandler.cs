using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.Helpdesk.Events;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>Same escalation target and out-of-band read pattern as
/// <see cref="TicketSlaBreachedNotificationHandler"/> — an earlier heads-up, before the SLA is
/// actually breached.</summary>
public sealed class TicketSlaWarningRaisedNotificationHandler : INotificationHandler<DomainEventNotification<TicketSlaWarningRaised>>
{
    private readonly IReadRepositoryAdmin<Ticket> _tickets;
    private readonly IReadRepositoryAdmin<TicketCategory> _categories;
    private readonly IReadRepositoryAdmin<Department> _departments;
    private readonly INotificationDispatcher _notificationDispatcher;

    public TicketSlaWarningRaisedNotificationHandler(
        IReadRepositoryAdmin<Ticket> tickets, IReadRepositoryAdmin<TicketCategory> categories, IReadRepositoryAdmin<Department> departments,
        INotificationDispatcher notificationDispatcher)
    {
        _tickets = tickets;
        _categories = categories;
        _departments = departments;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task Handle(DomainEventNotification<TicketSlaWarningRaised> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var tickets = await _tickets.ListIgnoringFiltersAsync(new TicketByIdSpecification(domainEvent.TicketId), cancellationToken);
        var ticket = tickets.FirstOrDefault(t => t.TenantId == domainEvent.TenantId);
        if (ticket is null)
        {
            return;
        }

        var categories = await _categories.ListIgnoringFiltersAsync(new TicketCategoryByIdSpecification(ticket.CategoryId), cancellationToken);
        var category = categories.FirstOrDefault(c => c.TenantId == domainEvent.TenantId);
        if (category is null)
        {
            return;
        }

        var departments = await _departments.ListIgnoringFiltersAsync(new DepartmentByIdSpecification(category.DepartmentId), cancellationToken);
        var department = departments.FirstOrDefault(d => d.TenantId == domainEvent.TenantId);
        if (department?.HeadEmployeeId is not { } headEmployeeId)
        {
            return;
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                headEmployeeId.Value.ToString(),
                "SLA approaching breach",
                $"Ticket \"{ticket.Subject}\" is approaching its SLA due time ({domainEvent.DueAt:u}).",
                new Dictionary<string, string> { ["ticketId"] = ticket.Id.Value.ToString() }),
            cancellationToken);
    }
}
