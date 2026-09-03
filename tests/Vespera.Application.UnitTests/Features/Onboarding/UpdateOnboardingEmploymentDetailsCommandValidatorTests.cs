using FluentValidation.TestHelper;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class UpdateOnboardingEmploymentDetailsCommandValidatorTests
{
    private readonly UpdateOnboardingEmploymentDetailsCommandValidator _validator = new();

    private static UpdateOnboardingEmploymentDetailsCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1));

    [Fact]
    public void Should_Have_Error_When_OnboardingDraftId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { OnboardingDraftId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.OnboardingDraftId);
    }

    [Fact]
    public void Should_Have_Error_When_DepartmentId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { DepartmentId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.DepartmentId);
    }

    [Fact]
    public void Should_Have_Error_When_DesignationId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { DesignationId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.DesignationId);
    }

    [Fact]
    public void Should_Have_Error_When_LocationId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { LocationId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(c => c.LocationId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
