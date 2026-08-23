using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RegisterBiometricDeviceCommandHandlerTests
{
    private readonly IWriteRepository<BiometricDevice> _devices = Substitute.For<IWriteRepository<BiometricDevice>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public RegisterBiometricDeviceCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private RegisterBiometricDeviceCommandHandler CreateHandler() => new(_devices, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Register_The_Device_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new RegisterBiometricDeviceCommand(Guid.NewGuid(), "ZKTeco", "device.local", 4370, "Biometric:ZkTeco:ApiKey", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _devices.Received(1).AddAsync(
            Arg.Is<BiometricDevice>(d => d.Host == "device.local" && d.Port == 4370 && d.VendorType == BiometricVendorType.ZKTeco),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Not_Add_When_Port_Is_Invalid()
    {
        var handler = CreateHandler();
        var command = new RegisterBiometricDeviceCommand(Guid.NewGuid(), "ZKTeco", "device.local", 0, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _devices.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
