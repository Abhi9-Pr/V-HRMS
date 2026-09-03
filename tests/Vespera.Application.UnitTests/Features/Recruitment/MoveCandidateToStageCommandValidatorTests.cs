using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class MoveCandidateToStageCommandValidatorTests
{
    private readonly MoveCandidateToStageCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CandidateId_Is_Empty()
    {
        var result = _validator.TestValidate(new MoveCandidateToStageCommand(Guid.Empty, Guid.NewGuid(), null));

        result.ShouldHaveValidationErrorFor(c => c.CandidateId);
    }

    [Fact]
    public void Should_Have_Error_When_TargetStageId_Is_Empty()
    {
        var result = _validator.TestValidate(new MoveCandidateToStageCommand(Guid.NewGuid(), Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.TargetStageId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new MoveCandidateToStageCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
