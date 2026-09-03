using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetSlaPoliciesQueryHandlerTests
{
    private readonly IReadRepository<SlaPolicy> _policies = Substitute.For<IReadRepository<SlaPolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var policy = SlaPolicy.Create(
            tenantId, "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(10), DateTimeOffset.UtcNow, "system",
            new TimeOnly(9, 0), new TimeOnly(18, 0)).Value;

        _policies.ListAsync(Arg.Any<ISpecification<SlaPolicy>>(), Arg.Any<CancellationToken>()).Returns([policy]);
        _policies.CountAsync(Arg.Any<ISpecification<SlaPolicy>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetSlaPoliciesQueryHandler(_policies, _tenantContext);
        var result = await handler.Handle(new GetSlaPoliciesQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Standard" && dto.Id == policy.Id.Value);
    }
}
