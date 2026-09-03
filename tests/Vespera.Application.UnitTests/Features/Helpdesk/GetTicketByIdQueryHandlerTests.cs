using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetTicketByIdQueryHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private readonly TenantId _tenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 5, 10, 0, 0, TimeSpan.Zero);

    private GetTicketByIdQueryHandler CreateHandler() => new(_tickets);

    [Fact]
    public async Task Handle_Should_Return_The_Mapped_Ticket()
    {
        var ticket = Ticket.Raise(
            _tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, Now, TimeSpan.FromHours(24)).Value;
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(ticket);

        var result = await CreateHandler().Handle(new GetTicketByIdQuery(ticket.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(ticket.Id.Value);
        result.Value.Subject.Should().Be("Subject");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Ticket_Does_Not_Exist()
    {
        _tickets.FirstOrDefaultAsync(Arg.Any<TicketByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        var result = await CreateHandler().Handle(new GetTicketByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket.not_found");
    }
}
