using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class SubmitOnboardingCommandValidatorTests
{
    private readonly SubmitOnboardingCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new SubmitOnboardingCommand(Guid.NewGuid(), "EMP-900"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeCode_Is_Empty()
    {
        var result = _validator.TestValidate(new SubmitOnboardingCommand(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(command => command.EmployeeCode);
    }
}
