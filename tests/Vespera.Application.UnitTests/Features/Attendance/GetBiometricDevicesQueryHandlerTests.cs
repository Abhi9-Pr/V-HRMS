using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetBiometricDevicesQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<BiometricDevice> _devices = Substitute.For<IReadRepository<BiometricDevice>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public GetBiometricDevicesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetBiometricDevicesQueryHandler CreateHandler() => new(_devices, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Paged_Result_Mapped_From_The_Devices()
    {
        var device = BiometricDevice.Register(
            TenantId, LocationId.New(), BiometricVendorType.ZKTeco, "10.0.0.5", 4370, null, Now, "admin").Value;

        _devices.ListAsync(Arg.Any<BiometricDevicesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([device]);
        _devices.CountAsync(Arg.Any<BiometricDevicesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var query = new GetBiometricDevicesQuery(new PagedRequest(1, 20));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto => dto.Id == device.Id.Value && dto.Host == "10.0.0.5" && dto.Port == 4370);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_Page_When_There_Are_No_Devices()
    {
        _devices.ListAsync(Arg.Any<BiometricDevicesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
        _devices.CountAsync(Arg.Any<BiometricDevicesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(0);

        var query = new GetBiometricDevicesQuery(new PagedRequest(1, 20));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
