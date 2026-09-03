using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UploadOnboardingDocumentCommandValidatorTests
{
    private readonly UploadOnboardingDocumentCommandValidator _validator = new();

    private static UploadOnboardingDocumentCommand ValidCommand() => new(
        Guid.NewGuid(), EmployeeDocumentType.Id, "id-card.png", [1, 2, 3]);

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { OnboardingDraftId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.OnboardingDraftId);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { FileName = string.Empty });

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Too_Long()
    {
        var result = _validator.TestValidate(ValidCommand() with { FileName = new string('f', 257) });

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { Content = [] });

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
