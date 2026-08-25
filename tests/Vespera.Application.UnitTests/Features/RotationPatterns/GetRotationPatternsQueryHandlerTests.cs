using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.RotationPatterns;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class GetRotationPatternsQueryHandlerTests
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns = Substitute.For<IReadRepository<RotationPattern>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var pattern = RotationPattern.Create(
            tenantId, "Pattern", [new RotationPatternDay(0, null)], DateTimeOffset.UtcNow, "system").Value;

        _rotationPatterns.ListAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns(new List<RotationPattern> { pattern });
        _rotationPatterns.CountAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = new GetRotationPatternsQueryHandler(_rotationPatterns, _tenantContext);
        var result = await handler.Handle(new GetRotationPatternsQuery(new PagedRequest()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Name == "Pattern" && dto.Id == pattern.Id.Value);
    }
}
