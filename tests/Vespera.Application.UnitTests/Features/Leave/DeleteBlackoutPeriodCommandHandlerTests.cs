using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class DeleteBlackoutPeriodCommandHandlerTests
{
    private readonly IReadRepository<BlackoutPeriod> _blackouts = Substitute.For<IReadRepository<BlackoutPeriod>>();
    private readonly IWriteRepository<BlackoutPeriod> _writer = Substitute.For<IWriteRepository<BlackoutPeriod>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private DeleteBlackoutPeriodCommandHandler CreateHandler() => new(_blackouts, _writer, _tenantContext, _currentUser, _dateTimeProvider);

    private BlackoutPeriod CreateBlackoutPeriod() => BlackoutPeriod.Create(
        _tenantId, DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25)).Value, "Festival season", null,
        DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Delete_The_BlackoutPeriod()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var blackout = CreateBlackoutPeriod();
        _blackouts.FirstOrDefaultAsync(Arg.Any<BlackoutPeriodByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(blackout);

        var result = await CreateHandler().Handle(new DeleteBlackoutPeriodCommand(blackout.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        blackout.IsDeleted.Should().BeTrue();
        _writer.Received(1).Update(blackout);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_BlackoutPeriod_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _blackouts.FirstOrDefaultAsync(Arg.Any<BlackoutPeriodByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((BlackoutPeriod?)null);

        var result = await CreateHandler().Handle(new DeleteBlackoutPeriodCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("blackout_period.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Deleted()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var blackout = CreateBlackoutPeriod();
        blackout.Delete(DateTimeOffset.UtcNow, "system");
        _blackouts.FirstOrDefaultAsync(Arg.Any<BlackoutPeriodByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(blackout);

        var result = await CreateHandler().Handle(new DeleteBlackoutPeriodCommand(blackout.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("entity.already_deleted");
    }
}
