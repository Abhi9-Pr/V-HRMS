using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class DeviceRegistrationTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly UserId UserId = UserId.New();

    [Fact]
    public void Register_Should_Succeed_And_Trim_DeviceId()
    {
        var result = DeviceRegistration.Register(TenantId, UserId, "  device-1  ", DevicePlatform.Ios, "push-token");

        result.IsSuccess.Should().BeTrue();
        result.Value.DeviceId.Should().Be("device-1");
        result.Value.Platform.Should().Be(DevicePlatform.Ios);
        result.Value.PushToken.Should().Be("push-token");
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Should_Fail_When_DeviceId_Is_Blank(string deviceId)
    {
        var result = DeviceRegistration.Register(TenantId, UserId, deviceId, DevicePlatform.Android, "push-token");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.device_id_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Should_Fail_When_PushToken_Is_Blank(string pushToken)
    {
        var result = DeviceRegistration.Register(TenantId, UserId, "device-1", DevicePlatform.Web, pushToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.push_token_required");
    }

    [Fact]
    public void UpdatePushToken_Should_Succeed()
    {
        var device = CreateDevice();

        var result = device.UpdatePushToken("new-token");

        result.IsSuccess.Should().BeTrue();
        device.PushToken.Should().Be("new-token");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePushToken_Should_Fail_When_Blank(string pushToken)
    {
        var device = CreateDevice();

        var result = device.UpdatePushToken(pushToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.push_token_required");
    }

    [Fact]
    public void Deactivate_Should_Succeed_When_Active()
    {
        var device = CreateDevice();

        var result = device.Deactivate();

        result.IsSuccess.Should().BeTrue();
        device.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_Should_Fail_When_Already_Inactive()
    {
        var device = CreateDevice();
        device.Deactivate();

        var result = device.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.already_inactive");
    }

    [Fact]
    public void Reactivate_Should_Set_IsActive_True()
    {
        var device = CreateDevice();
        device.Deactivate();

        device.Reactivate();

        device.IsActive.Should().BeTrue();
    }

    private static DeviceRegistration CreateDevice() =>
        DeviceRegistration.Register(TenantId, UserId, "device-1", DevicePlatform.Ios, "push-token").Value;
}
