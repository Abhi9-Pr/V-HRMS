using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class ResolveTicketCommandHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    public ResolveTicketCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private ResolveTicketCommandHandler CreateHandler() => new(_tickets, _dateTimeProvider);

    private Ticket CreateTicket()
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_Should_Resolve_An_Open_Ticket()
    {
        var ticket = CreateTicket();

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveTicketCommand(ticket.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.Resolved);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Is_Already_Resolved()
    {
        var ticket = CreateTicket();
        ticket.Resolve(Now);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveTicketCommand(ticket.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Does_Not_Exist()
    {
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResolveTicketCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.not_found");
    }
}
