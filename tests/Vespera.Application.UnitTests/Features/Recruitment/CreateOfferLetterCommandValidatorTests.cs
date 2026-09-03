using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateOfferLetterCommandValidatorTests
{
    private readonly CreateOfferLetterCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CandidateId_Is_Empty()
    {
        var command = new CreateOfferLetterCommand(Guid.Empty, Guid.NewGuid(), 1200000m, Currency.Inr, new DateOnly(2026, 6, 1), null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CandidateId);
    }

    [Fact]
    public void Should_Have_Error_When_ProposedCtc_Is_Not_Positive()
    {
        var command = new CreateOfferLetterCommand(Guid.NewGuid(), Guid.NewGuid(), 0m, Currency.Inr, new DateOnly(2026, 6, 1), null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ProposedCtc);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateOfferLetterCommand(Guid.NewGuid(), Guid.NewGuid(), 1200000m, Currency.Inr, new DateOnly(2026, 6, 1), null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
