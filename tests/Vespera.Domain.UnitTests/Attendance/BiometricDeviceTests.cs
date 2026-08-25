using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Attendance;

public class BiometricDeviceTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_Should_Succeed_With_Valid_Details()
    {
        var result = Register();

        result.IsSuccess.Should().BeTrue();
        result.Value.Cursor.Should().BeNull();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Register_Should_Fail_With_A_Blank_Host()
    {
        var result = BiometricDevice.Register(
            TenantId, LocationId.New(), BiometricVendorType.ZKTeco, "  ", 4370, null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("biometric_device.host_required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(70000)]
    public void Register_Should_Fail_With_An_Invalid_Port(int port)
    {
        var result = BiometricDevice.Register(
            TenantId, LocationId.New(), BiometricVendorType.ZKTeco, "device.local", port, null, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("biometric_device.invalid_port");
    }

    [Fact]
    public void UpdateCursor_Should_Set_The_Cursor()
    {
        var device = Register().Value;

        var result = device.UpdateCursor("cursor-42", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        device.Cursor.Should().Be("cursor-42");
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var device = Register().Value;

        var result = device.Deactivate(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_Fail_When_Already_Inactive()
    {
        var device = Register().Value;
        device.Deactivate(Now, "hr@vespera.test");

        var result = device.Deactivate(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("biometric_device.already_inactive");
    }

    private static Result<BiometricDevice> Register() =>
        BiometricDevice.Register(TenantId, LocationId.New(), BiometricVendorType.ZKTeco, "device.local", 4370, "Biometric:ZkTeco:ApiKey", Now, "hr@vespera.test");
}
