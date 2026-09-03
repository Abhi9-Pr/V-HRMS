using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetOfferLettersForCandidateQueryValidatorTests
{
    private readonly GetOfferLettersForCandidateQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CandidateId_Is_Empty()
    {
        var result = _validator.TestValidate(new GetOfferLettersForCandidateQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.CandidateId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(new GetOfferLettersForCandidateQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
