using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class SubmitInterviewScorecardCommandValidatorTests
{
    private readonly SubmitInterviewScorecardCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_InterviewId_Is_Empty()
    {
        var result = _validator.TestValidate(new SubmitInterviewScorecardCommand(Guid.Empty, Guid.NewGuid(), 4, null, null));

        result.ShouldHaveValidationErrorFor(c => c.InterviewId);
    }

    [Fact]
    public void Should_Have_Error_When_InterviewerId_Is_Empty()
    {
        var result = _validator.TestValidate(new SubmitInterviewScorecardCommand(Guid.NewGuid(), Guid.Empty, 4, null, null));

        result.ShouldHaveValidationErrorFor(c => c.InterviewerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Should_Have_Error_When_Rating_Is_Out_Of_Range(int rating)
    {
        var result = _validator.TestValidate(new SubmitInterviewScorecardCommand(Guid.NewGuid(), Guid.NewGuid(), rating, null, null));

        result.ShouldHaveValidationErrorFor(c => c.Rating);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new SubmitInterviewScorecardCommand(Guid.NewGuid(), Guid.NewGuid(), 4, null, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
