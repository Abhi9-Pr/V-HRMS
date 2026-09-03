using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class GetExpenseClaimByIdQueryValidatorTests
{
    private readonly GetExpenseClaimByIdQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new GetExpenseClaimByIdQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Set()
    {
        var result = _validator.TestValidate(new GetExpenseClaimByIdQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
