using FluentValidation.TestHelper;
using Vespera.Application.Features.Locations;

namespace Vespera.Application.UnitTests.Features.Locations;

public class CreateLocationCommandValidatorTests
{
    private readonly CreateLocationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateLocationCommand(string.Empty, "1 MG Road", "Bengaluru", "India", 12.9716, 77.5946, "Asia/Kolkata", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_TimeZoneId_Is_Empty()
    {
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 12.9716, 77.5946, string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TimeZoneId);
    }

    [Fact]
    public void Should_Have_Error_When_Latitude_Is_Out_Of_Range()
    {
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 200, 77.5946, "Asia/Kolkata", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Fact]
    public void Should_Have_Error_When_Longitude_Is_Out_Of_Range()
    {
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 12.9716, 200, "Asia/Kolkata", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateLocationCommand("Head Office", "1 MG Road", "Bengaluru", "India", 12.9716, 77.5946, "Asia/Kolkata", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
