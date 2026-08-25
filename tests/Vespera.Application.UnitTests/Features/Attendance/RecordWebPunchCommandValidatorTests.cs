using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class RecordWebPunchCommandValidatorTests
{
    private readonly RecordWebPunchCommandValidator _validator = new();

    private static RecordWebPunchCommand ValidCommand() => new(Guid.NewGuid(), "In", 12.9716, 77.5946);

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Neither_Coordinate_Is_Supplied()
    {
        var result = _validator.TestValidate(ValidCommand() with { Latitude = null, Longitude = null });
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
    public void Should_Have_Error_When_Only_Latitude_Is_Supplied()
    {
        var result = _validator.TestValidate(ValidCommand() with { Longitude = null });
        result.ShouldHaveValidationErrorFor("Location");
    }

    [Fact]
    public void Should_Have_Error_When_Only_Longitude_Is_Supplied()
    {
        var result = _validator.TestValidate(ValidCommand() with { Latitude = null });
        result.ShouldHaveValidationErrorFor("Location");
    }
}
