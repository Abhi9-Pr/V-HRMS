using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RegisterBiometricDeviceCommandValidatorTests
{
    private readonly RegisterBiometricDeviceCommandValidator _validator = new();

    private static RegisterBiometricDeviceCommand ValidCommand() =>
        new(Guid.NewGuid(), "ZKTeco", "10.0.0.5", 4370, null, null);

    [Fact]
    public void Should_Have_Error_When_LocationId_Is_Empty()
    {
        var command = ValidCommand() with { LocationId = Guid.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LocationId);
    }

    [Fact]
    public void Should_Have_Error_When_VendorType_Is_Not_A_Known_Vendor()
    {
        var command = ValidCommand() with { VendorType = "Unknown" };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.VendorType);
    }

    [Fact]
    public void Should_Have_Error_When_Host_Is_Empty()
    {
        var command = ValidCommand() with { Host = string.Empty };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Host);
    }

    [Fact]
    public void Should_Have_Error_When_Port_Is_Out_Of_Range()
    {
        var command = ValidCommand() with { Port = 70000 };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Port);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
