using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetCandidateByIdQueryValidatorTests
{
    private readonly GetCandidateByIdQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new GetCandidateByIdQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(new GetCandidateByIdQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
