using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class AssignTicketCommandHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    private AssignTicketCommandHandler CreateHandler() => new(_tickets);

    private Ticket CreateTicket()
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Assign_The_Ticket_To_The_Given_Employee()
    {
        var ticket = CreateTicket();
        var employeeId = EmployeeId.New();

        var handler = CreateHandler();
        var result = await handler.Handle(new AssignTicketCommand(ticket.Id.Value, employeeId.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.AssignedTo.Should().Be(employeeId);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Does_Not_Exist()
    {
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Is_Already_Closed()
    {
        var ticket = CreateTicket();
        ticket.Resolve(Now);
        ticket.Close();

        var handler = CreateHandler();
        var result = await handler.Handle(new AssignTicketCommand(ticket.Id.Value, Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
