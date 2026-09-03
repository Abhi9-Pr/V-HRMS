using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Leave;

public class GetBlackoutPeriodsQueryHandlerTests
{
    private readonly IReadRepository<BlackoutPeriod> _blackouts = Substitute.For<IReadRepository<BlackoutPeriod>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private GetBlackoutPeriodsQueryHandler CreateHandler() => new(_blackouts, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_BlackoutPeriods_For_The_Current_Tenant()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        var blackout = BlackoutPeriod.Create(
            tenantId, DateRange.Create(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25)).Value, "Festival season", null, Now, "system").Value;
        _blackouts.ListAsync(Arg.Any<BlackoutPeriodsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new List<BlackoutPeriod> { blackout });

        var result = await CreateHandler().Handle(new GetBlackoutPeriodsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.Id == blackout.Id.Value && d.Reason == "Festival season");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_BlackoutPeriods()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _blackouts.ListAsync(Arg.Any<BlackoutPeriodsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns(new List<BlackoutPeriod>());

        var result = await CreateHandler().Handle(new GetBlackoutPeriodsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
