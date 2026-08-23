using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.RotationPatterns;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class CreateRotationPatternCommandHandlerTests
{
    private readonly IWriteRepository<RotationPattern> _rotationPatterns = Substitute.For<IWriteRepository<RotationPattern>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateRotationPatternCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateRotationPatternCommandHandler CreateHandler() =>
        new(_rotationPatterns, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_RotationPattern_And_Return_Its_Id_On_Success()
    {
        var shiftId = Guid.NewGuid();
        var handler = CreateHandler();
        var command = new CreateRotationPatternCommand(
            "3-Day Rotation",
            [new RotationPatternDayRequest(0, shiftId), new RotationPatternDayRequest(1, null)],
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _rotationPatterns.Received(1).AddAsync(
            Arg.Is<RotationPattern>(p => p.Name == "3-Day Rotation" && p.Days.Count == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Sequence_Has_A_Gap()
    {
        var handler = CreateHandler();
        var command = new CreateRotationPatternCommand(
            "Gappy", [new RotationPatternDayRequest(0, null), new RotationPatternDayRequest(2, null)], null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _rotationPatterns.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
