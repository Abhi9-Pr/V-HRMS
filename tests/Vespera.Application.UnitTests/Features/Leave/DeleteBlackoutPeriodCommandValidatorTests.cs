using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class DeleteBlackoutPeriodCommandValidatorTests
{
    private readonly DeleteBlackoutPeriodCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new DeleteBlackoutPeriodCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new DeleteBlackoutPeriodCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
