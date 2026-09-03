using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CompleteOffboardingChecklistItemCommandValidatorTests
{
    private readonly CompleteOffboardingChecklistItemCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ChecklistId_Is_Empty()
    {
        var result = _validator.TestValidate(new CompleteOffboardingChecklistItemCommand(Guid.Empty, 0, null));

        result.ShouldHaveValidationErrorFor(c => c.ChecklistId);
    }

    [Fact]
    public void Should_Have_Error_When_ItemIndex_Is_Negative()
    {
        var result = _validator.TestValidate(new CompleteOffboardingChecklistItemCommand(Guid.NewGuid(), -1, null));

        result.ShouldHaveValidationErrorFor(c => c.ItemIndex);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new CompleteOffboardingChecklistItemCommand(Guid.NewGuid(), 0, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
