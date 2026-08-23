using FluentValidation.TestHelper;
using Vespera.Application.Features.Locations;

namespace Vespera.Application.UnitTests.Features.Locations;

public class UpdateLocationCommandValidatorTests
{
    private readonly UpdateLocationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateLocationCommand(Guid.Empty, 12.9716, 77.5946, "1 MG Road", "Bengaluru", "India");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Longitude_Is_Out_Of_Range()
    {
        var command = new UpdateLocationCommand(Guid.NewGuid(), 12.9716, 200, "1 MG Road", "Bengaluru", "India");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateLocationCommand(Guid.NewGuid(), 12.9716, 77.5946, "1 MG Road", "Bengaluru", "India");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
