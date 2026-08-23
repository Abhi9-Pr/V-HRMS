using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.RotationPatterns;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class UpdateRotationPatternCommandHandlerTests
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns = Substitute.For<IReadRepository<RotationPattern>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateRotationPatternCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateRotationPatternCommandHandler CreateHandler() =>
        new(_rotationPatterns, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Reconfigure_Days_When_Pattern_Exists()
    {
        var pattern = RotationPattern.Create(
            _tenantId, "Pattern", [new RotationPatternDay(0, ShiftId.New())], DateTimeOffset.UtcNow, "seed").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>()).Returns(pattern);

        var handler = CreateHandler();
        var command = new UpdateRotationPatternCommand(
            pattern.Id.Value, [new RotationPatternDayRequest(0, null), new RotationPatternDayRequest(1, null)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        pattern.Days.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Pattern_Missing()
    {
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns((RotationPattern?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateRotationPatternCommand(Guid.NewGuid(), [new RotationPatternDayRequest(0, null)]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("rotation_pattern.not_found");
    }
}
