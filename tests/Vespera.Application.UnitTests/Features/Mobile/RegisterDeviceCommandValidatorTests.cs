using FluentValidation.TestHelper;
using Vespera.Application.Features.Mobile;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Mobile;

public class RegisterDeviceCommandValidatorTests
{
    private readonly RegisterDeviceCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Empty()
    {
        var result = _validator.TestValidate(new RegisterDeviceCommand(string.Empty, DevicePlatform.Ios, "token"));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new RegisterDeviceCommand(new string('a', 257), DevicePlatform.Ios, "token"));

        result.ShouldHaveValidationErrorFor(c => c.DeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_PushToken_Is_Empty()
    {
        var result = _validator.TestValidate(new RegisterDeviceCommand("device-1", DevicePlatform.Ios, string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.PushToken);
    }

    [Fact]
    public void Should_Have_Error_When_Platform_Is_Not_A_Known_Value()
    {
        var result = _validator.TestValidate(new RegisterDeviceCommand("device-1", (DevicePlatform)999, "token"));

        result.ShouldHaveValidationErrorFor(c => c.Platform);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RegisterDeviceCommand("device-1", DevicePlatform.Android, "token"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
