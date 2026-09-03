using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class DecideExpenseApprovalCommandValidatorTests
{
    private readonly DecideExpenseApprovalCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ClaimId_Is_Empty()
    {
        var result = _validator.TestValidate(new DecideExpenseApprovalCommand(Guid.Empty, true, null, null));

        result.ShouldHaveValidationErrorFor(c => c.ClaimId);
    }

    [Fact]
    public void Should_Have_Error_When_Rejected_Without_A_Comment()
    {
        var result = _validator.TestValidate(new DecideExpenseApprovalCommand(Guid.NewGuid(), false, null, null));

        result.ShouldHaveValidationErrorFor(c => c.Comment);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Approved_Without_A_Comment()
    {
        var result = _validator.TestValidate(new DecideExpenseApprovalCommand(Guid.NewGuid(), true, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Rejected_With_A_Comment()
    {
        var result = _validator.TestValidate(new DecideExpenseApprovalCommand(Guid.NewGuid(), false, "Not a valid expense", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
