using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.Helpdesk.Events;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class TicketSlaWarningRaisedNotificationHandlerTests
{
    private readonly IReadRepositoryAdmin<Ticket> _tickets = Substitute.For<IReadRepositoryAdmin<Ticket>>();
    private readonly IReadRepositoryAdmin<TicketCategory> _categories = Substitute.For<IReadRepositoryAdmin<TicketCategory>>();
    private readonly IReadRepositoryAdmin<Department> _departments = Substitute.For<IReadRepositoryAdmin<Department>>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    private TicketSlaWarningRaisedNotificationHandler CreateHandler() => new(_tickets, _categories, _departments, _notificationDispatcher);

    private (Ticket Ticket, TicketCategory Category, Department Department) SeedTicketChain(EmployeeId? headEmployeeId)
    {
        var department = Department.Create(_tenantId, "IT", "IT", null, Now, "admin@vespera.test").Value;
        if (headEmployeeId is { } head)
        {
            department.AssignHead(head, Now, "admin@vespera.test");
        }

        var category = TicketCategory.Create(_tenantId, "Hardware", department.Id, null, Now, "admin@vespera.test").Value;
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), category.Id, SlaPolicyId.New(), "Laptop broken", "Description",
            TicketPriority.High, Now, TimeSpan.FromHours(4)).Value;

        _tickets.ListIgnoringFiltersAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([ticket]);
        _categories.ListIgnoringFiltersAsync(Arg.Any<TicketCategoryByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([category]);
        _departments.ListIgnoringFiltersAsync(Arg.Any<DepartmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns([department]);

        return (ticket, category, department);
    }

    [Fact]
    public async Task Handle_Should_Notify_The_Department_Head_When_Everything_Resolves()
    {
        var headId = EmployeeId.New();
        var (ticket, _, _) = SeedTicketChain(headId);

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<TicketSlaWarningRaised>(new TicketSlaWarningRaised(ticket.Id, _tenantId, ticket.DueAt, Now)),
            CancellationToken.None);

        await _notificationDispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.RecipientId == headId.Value.ToString() && m.Metadata["ticketId"] == ticket.Id.Value.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Ticket_Is_Not_Found()
    {
        _tickets.ListIgnoringFiltersAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<Ticket>());

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<TicketSlaWarningRaised>(new TicketSlaWarningRaised(TicketId.New(), _tenantId, Now, Now)),
            CancellationToken.None);

        await _notificationDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Do_Nothing_When_The_Department_Has_No_Head()
    {
        var (ticket, _, _) = SeedTicketChain(headEmployeeId: null);

        var handler = CreateHandler();
        await handler.Handle(
            new DomainEventNotification<TicketSlaWarningRaised>(new TicketSlaWarningRaised(ticket.Id, _tenantId, ticket.DueAt, Now)),
            CancellationToken.None);

        await _notificationDispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }
}
