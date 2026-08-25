using FluentValidation.TestHelper;
using Vespera.Application.Features.Shifts;

namespace Vespera.Application.UnitTests.Features.Shifts;

public class DeleteShiftCommandValidatorTests
{
    private readonly DeleteShiftCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new DeleteShiftCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        var result = _validator.TestValidate(new DeleteShiftCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
