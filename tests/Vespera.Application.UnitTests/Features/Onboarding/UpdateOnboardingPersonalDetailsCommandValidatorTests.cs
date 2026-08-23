using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UpdateOnboardingPersonalDetailsCommandValidatorTests
{
    private readonly UpdateOnboardingPersonalDetailsCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(
            new UpdateOnboardingPersonalDetailsCommand(Guid.NewGuid(), "Ada", "Lovelace", "ada@vespera.test", "+14155552671", new DateOnly(1990, 1, 1)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var result = _validator.TestValidate(
            new UpdateOnboardingPersonalDetailsCommand(Guid.NewGuid(), "", "Lovelace", "ada@vespera.test", "+14155552671", new DateOnly(1990, 1, 1)));
        result.ShouldHaveValidationErrorFor(command => command.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(
            new UpdateOnboardingPersonalDetailsCommand(Guid.Empty, "Ada", "Lovelace", "ada@vespera.test", "+14155552671", new DateOnly(1990, 1, 1)));
        result.ShouldHaveValidationErrorFor(command => command.OnboardingDraftId);
    }
}
