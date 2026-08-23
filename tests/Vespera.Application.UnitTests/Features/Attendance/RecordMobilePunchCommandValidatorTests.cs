using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RecordMobilePunchCommandValidatorTests
{
    private readonly RecordMobilePunchCommandValidator _validator = new();

    private static RecordMobilePunchCommand ValidCommand() =>
        new(Guid.NewGuid(), "In", 12.9716, 77.5946, 10, false, "device-1", true, 250, 100, "idem-key-1");

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { EmployeeId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_PunchType_Is_Unrecognized()
    {
        var result = _validator.TestValidate(ValidCommand() with { PunchType = "Sideways" });
        result.ShouldHaveValidationErrorFor(command => command.PunchType);
    }

    [Fact]
    public void Should_Have_Error_When_DeviceId_Is_Blank()
    {
        var result = _validator.TestValidate(ValidCommand() with { DeviceId = "" });
        result.ShouldHaveValidationErrorFor(command => command.DeviceId);
    }

    [Fact]
    public void Should_Have_Error_When_Accuracy_Is_Negative()
    {
        var result = _validator.TestValidate(ValidCommand() with { Accuracy = -1 });
        result.ShouldHaveValidationErrorFor(command => command.Accuracy);
    }

    [Fact]
    public void Should_Have_Error_When_IdempotencyKey_Is_Missing()
    {
        var result = _validator.TestValidate(ValidCommand() with { IdempotencyKey = null });
        result.ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
    }

    [Fact]
    public void Should_Have_Error_When_IdempotencyKey_Is_Blank()
    {
        var result = _validator.TestValidate(ValidCommand() with { IdempotencyKey = "   " });
        result.ShouldHaveValidationErrorFor(command => command.IdempotencyKey);
    }

    [Fact]
    public void Should_Have_Error_When_Only_Latitude_Is_Supplied()
    {
        var result = _validator.TestValidate(ValidCommand() with { Longitude = null });
        result.ShouldHaveValidationErrorFor("Location");
    }
}
