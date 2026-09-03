using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateBlackoutPeriodCommandHandlerTests
{
    private readonly IWriteRepository<BlackoutPeriod> _writer = Substitute.For<IWriteRepository<BlackoutPeriod>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private CreateBlackoutPeriodCommandHandler CreateHandler() => new(_writer, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_A_BlackoutPeriod_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _currentUser.Email.Returns("hr@demo.vespera.test");

        var command = new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25), "Festival season", null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _writer.Received(1).AddAsync(Arg.Is<BlackoutPeriod>(b => b.Reason == "Festival season"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Period_Is_Invalid()
    {
        _tenantContext.TenantId.Returns(TenantId.New());

        var command = new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 20), "Festival season", null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _writer.DidNotReceive().AddAsync(Arg.Any<BlackoutPeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Reason_Is_Blank()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var command = new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25), "   ", null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("blackout_period.reason_required");
    }
}
