using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetQuarantinedPunchesQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset PunchedAtUtc = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<QuarantinedBiometricPunch> _entries = Substitute.For<IReadRepository<QuarantinedBiometricPunch>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public GetQuarantinedPunchesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetQuarantinedPunchesQueryHandler CreateHandler() => new(_entries, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Paged_Result_Mapped_From_The_Entries()
    {
        var entry = QuarantinedBiometricPunch.Create(TenantId, BiometricDeviceId.New(), "ZK-042", PunchedAtUtc, PunchType.In, "rec-1");
        _entries.ListAsync(Arg.Any<QuarantinedBiometricPunchesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([entry]);
        _entries.CountAsync(Arg.Any<QuarantinedBiometricPunchesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var query = new GetQuarantinedPunchesQuery(new PagedRequest(1, 20));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Id == entry.Id.Value && dto.DeviceUserId == "ZK-042");
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_Page_When_There_Are_No_Quarantined_Punches()
    {
        _entries.ListAsync(Arg.Any<QuarantinedBiometricPunchesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _entries.CountAsync(Arg.Any<QuarantinedBiometricPunchesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(0);

        var query = new GetQuarantinedPunchesQuery(new PagedRequest(1, 20));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
