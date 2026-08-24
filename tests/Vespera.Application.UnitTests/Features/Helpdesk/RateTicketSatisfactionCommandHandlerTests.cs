using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class RateTicketSatisfactionCommandHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    private RateTicketSatisfactionCommandHandler CreateHandler() => new(_tickets);

    private Ticket CreateClosedTicket()
    {
        var ticket = Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        ticket.Resolve(Now.AddHours(1));
        ticket.Close();
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Set_The_Rating_On_A_Closed_Ticket()
    {
        var ticket = CreateClosedTicket();

        var handler = CreateHandler();
        var result = await handler.Handle(new RateTicketSatisfactionCommand(ticket.Id.Value, 5, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.SatisfactionRating.Should().Be(5);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Is_Not_Closed()
    {
        var ticket = Ticket.Raise(
            TenantId.New(), EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var handler = CreateHandler();
        var result = await handler.Handle(new RateTicketSatisfactionCommand(ticket.Id.Value, 5, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
