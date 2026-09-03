using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetTicketsQueryHandlerTests
{
    private readonly IReadRepository<Ticket> _tickets = Substitute.For<IReadRepository<Ticket>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var ticket = Ticket.Raise(
            tenantId, EmployeeId.New(), TicketCategoryId.New(), SlaPolicyId.New(), "Subject", "Description",
            TicketPriority.Low, DateTimeOffset.UtcNow, TimeSpan.FromHours(24)).Value;

        _tickets.ListAsync(Arg.Any<ISpecification<Ticket>>(), Arg.Any<CancellationToken>()).Returns([ticket]);
        _tickets.CountAsync(Arg.Any<ISpecification<Ticket>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetTicketsQueryHandler(_tickets, _tenantContext);
        var result = await handler.Handle(new GetTicketsQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Subject == "Subject" && dto.Id == ticket.Id.Value);
    }
}
