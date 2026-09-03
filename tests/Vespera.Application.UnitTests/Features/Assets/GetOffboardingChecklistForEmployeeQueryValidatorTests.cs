using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetOffboardingChecklistForEmployeeQueryValidatorTests
{
    private readonly GetOffboardingChecklistForEmployeeQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(new GetOffboardingChecklistForEmployeeQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_EmployeeId_Is_Set()
    {
        var result = _validator.TestValidate(new GetOffboardingChecklistForEmployeeQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
