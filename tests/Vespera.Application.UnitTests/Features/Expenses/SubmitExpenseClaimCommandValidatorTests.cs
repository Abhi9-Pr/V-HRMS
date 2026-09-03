using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class SubmitExpenseClaimCommandValidatorTests
{
    private readonly SubmitExpenseClaimCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ClaimId_Is_Empty()
    {
        var result = _validator.TestValidate(new SubmitExpenseClaimCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.ClaimId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new SubmitExpenseClaimCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
