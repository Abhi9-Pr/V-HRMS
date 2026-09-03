using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class ConfirmOnboardingDocumentFieldsCommandValidatorTests
{
    private readonly ConfirmOnboardingDocumentFieldsCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(new ConfirmOnboardingDocumentFieldsCommand(Guid.Empty, Guid.NewGuid(), "Jane", "Doe", null));

        result.ShouldHaveValidationErrorFor(c => c.OnboardingDraftId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeDocumentId_Is_Empty()
    {
        var result = _validator.TestValidate(new ConfirmOnboardingDocumentFieldsCommand(Guid.NewGuid(), Guid.Empty, "Jane", "Doe", null));

        result.ShouldHaveValidationErrorFor(c => c.EmployeeDocumentId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ConfirmOnboardingDocumentFieldsCommand(Guid.NewGuid(), Guid.NewGuid(), "Jane", "Doe", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
