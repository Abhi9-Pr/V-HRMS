using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.Helpdesk.Events;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>Escalates a breached ticket to its category's department head. This runs out-of-band —
/// replayed by the outbox dispatcher from a background scope with no HTTP request — so, exactly
/// like <c>EmployeeExitedNotificationHandler</c> (11b), every read goes through
/// <see cref="IReadRepositoryAdmin{T}"/> with the tenant id taken directly off the event rather
/// than relying on the ambient tenant context, which is unpopulated here.</summary>
public sealed class TicketSlaBreachedNotificationHandler : INotificationHandler<DomainEventNotification<TicketSlaBreached>>
{
    private readonly IReadRepositoryAdmin<Ticket> _tickets;
    private readonly IReadRepositoryAdmin<TicketCategory> _categories;
    private readonly IReadRepositoryAdmin<Department> _departments;
    private readonly INotificationDispatcher _notificationDispatcher;

    public TicketSlaBreachedNotificationHandler(
        IReadRepositoryAdmin<Ticket> tickets, IReadRepositoryAdmin<TicketCategory> categories, IReadRepositoryAdmin<Department> departments,
        INotificationDispatcher notificationDispatcher)
    {
        _tickets = tickets;
        _categories = categories;
        _departments = departments;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task Handle(DomainEventNotification<TicketSlaBreached> notification, CancellationToken cancellationToken)
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
                "SLA breached",
                $"Ticket \"{ticket.Subject}\" breached its SLA (was due {domainEvent.DueAt:u}).",
                new Dictionary<string, string> { ["ticketId"] = ticket.Id.Value.ToString() }),
            cancellationToken);
    }
}
