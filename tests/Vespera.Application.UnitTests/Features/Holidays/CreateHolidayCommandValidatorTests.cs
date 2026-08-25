using FluentValidation.TestHelper;
using Vespera.Application.Features.Holidays;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class CreateHolidayCommandValidatorTests
{
    private readonly CreateHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_LocationId_Is_Empty()
    {
        var command = new CreateHolidayCommand(Guid.Empty, new DateOnly(2026, 1, 26), "Republic Day", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LocationId);
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateHolidayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 26), string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateHolidayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 26), "Republic Day", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
