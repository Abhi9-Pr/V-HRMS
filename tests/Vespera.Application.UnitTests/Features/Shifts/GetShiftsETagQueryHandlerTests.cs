using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Rosters;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class GetShiftsETagQueryHandlerTests
{
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetShiftsETagQueryHandler CreateHandler() => new(_shifts, _tenantContext);

    private Shift CreateShift(string name) =>
        Shift.Create(_tenantId, name, new TimeOnly(9, 0), new TimeOnly(18, 0), 10, DateTimeOffset.UtcNow, "system").Value;

    [Fact]
    public async Task Handle_Should_Return_The_Same_Hash_For_The_Same_Shifts()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var shift = CreateShift("General");
        _shifts.ListAsync(Arg.Any<ShiftsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([shift]);

        var first = await CreateHandler().Handle(new GetShiftsETagQuery(), CancellationToken.None);
        var second = await CreateHandler().Handle(new GetShiftsETagQuery(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().NotBeNullOrEmpty();
        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Different_Hash_When_The_Shift_Set_Differs()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var shiftA = CreateShift("General");
        var shiftB = CreateShift("Night");

        _shifts.ListAsync(Arg.Any<ShiftsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([shiftA]);
        var first = await CreateHandler().Handle(new GetShiftsETagQuery(), CancellationToken.None);

        _shifts.ListAsync(Arg.Any<ShiftsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([shiftA, shiftB]);
        var second = await CreateHandler().Handle(new GetShiftsETagQuery(), CancellationToken.None);

        second.Value.Should().NotBe(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Hash_Even_When_There_Are_No_Shifts()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _shifts.ListAsync(Arg.Any<ShiftsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetShiftsETagQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }
}
