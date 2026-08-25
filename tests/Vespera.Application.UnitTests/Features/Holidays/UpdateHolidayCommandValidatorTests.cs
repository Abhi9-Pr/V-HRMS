using FluentValidation.TestHelper;
using Vespera.Application.Features.Holidays;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class UpdateHolidayCommandValidatorTests
{
    private readonly UpdateHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new UpdateHolidayCommand(Guid.Empty, "Republic Day", new DateOnly(2026, 1, 26));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new UpdateHolidayCommand(Guid.NewGuid(), string.Empty, new DateOnly(2026, 1, 26));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UpdateHolidayCommand(Guid.NewGuid(), "Republic Day", new DateOnly(2026, 1, 26));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
