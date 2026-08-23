using FluentValidation.TestHelper;
using Vespera.Application.Features.Holidays;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class DeleteHolidayCommandValidatorTests
{
    private readonly DeleteHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new DeleteHolidayCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        var result = _validator.TestValidate(new DeleteHolidayCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
