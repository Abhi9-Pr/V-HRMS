using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class AcceptOfferCommandValidatorTests
{
    private readonly AcceptOfferCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_OfferLetterId_Is_Empty()
    {
        var result = _validator.TestValidate(new AcceptOfferCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.OfferLetterId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new AcceptOfferCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
