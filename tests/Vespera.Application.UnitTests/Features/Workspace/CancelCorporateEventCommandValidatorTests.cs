using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CancelCorporateEventCommandValidatorTests
{
    private readonly CancelCorporateEventCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CorporateEventId_Is_Empty()
    {
        var result = _validator.TestValidate(new CancelCorporateEventCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.CorporateEventId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new CancelCorporateEventCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
