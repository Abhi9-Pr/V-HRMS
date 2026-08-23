using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.RotationPatterns;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class GetRotationPatternByIdQueryHandlerTests
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns = Substitute.For<IReadRepository<RotationPattern>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetRotationPatternByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task Handle_Should_Return_Mapped_Dto_With_Days_When_Pattern_Exists()
    {
        var shiftId = ShiftId.New();
        var pattern = RotationPattern.Create(
            _tenantId, "Pattern", [new RotationPatternDay(0, shiftId), new RotationPatternDay(1, null)], DateTimeOffset.UtcNow, "seed").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>()).Returns(pattern);

        var handler = new GetRotationPatternByIdQueryHandler(_rotationPatterns, _tenantContext);
        var result = await handler.Handle(new GetRotationPatternByIdQuery(pattern.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Days.Should().HaveCount(2);
        result.Value.Days[0].ShiftId.Should().Be(shiftId.Value);
        result.Value.Days[1].ShiftId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Pattern_Missing()
    {
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns((RotationPattern?)null);

        var handler = new GetRotationPatternByIdQueryHandler(_rotationPatterns, _tenantContext);
        var result = await handler.Handle(new GetRotationPatternByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("rotation_pattern.not_found");
    }
}
