using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class ExtractOnboardingDocumentFieldsCommandValidatorTests
{
    private readonly ExtractOnboardingDocumentFieldsCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(new ExtractOnboardingDocumentFieldsCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.OnboardingDraftId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeDocumentId_Is_Empty()
    {
        var result = _validator.TestValidate(new ExtractOnboardingDocumentFieldsCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.EmployeeDocumentId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ExtractOnboardingDocumentFieldsCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
