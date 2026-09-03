using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetCandidatePipelineQueryValidatorTests
{
    private readonly GetCandidatePipelineQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_JobRequisitionId_Is_Empty()
    {
        var result = _validator.TestValidate(new GetCandidatePipelineQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.JobRequisitionId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(new GetCandidatePipelineQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
