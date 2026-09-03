using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class RecordOnboardingConsentCommandValidatorTests
{
    private readonly RecordOnboardingConsentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordOnboardingConsentCommand(Guid.Empty, "DataProcessing"));

        result.ShouldHaveValidationErrorFor(c => c.OnboardingDraftId);
    }

    [Fact]
    public void Should_Have_Error_When_ConsentType_Is_Empty()
    {
        var result = _validator.TestValidate(new RecordOnboardingConsentCommand(Guid.NewGuid(), string.Empty));

        result.ShouldHaveValidationErrorFor(c => c.ConsentType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RecordOnboardingConsentCommand(Guid.NewGuid(), "DataProcessing"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
