using FluentValidation.TestHelper;
using Vespera.Application.Features.Shifts;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class UpdateShiftCommandValidatorTests
{
    private readonly UpdateShiftCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateShiftCommand(Guid.Empty, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new UpdateShiftCommand(Guid.NewGuid(), string.Empty, new TimeOnly(9, 0), new TimeOnly(18, 0), 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateShiftCommand(Guid.NewGuid(), "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 30);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
