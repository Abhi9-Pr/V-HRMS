using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CompleteInterviewCommandValidatorTests
{
    private readonly CompleteInterviewCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_InterviewId_Is_Empty()
    {
        var result = _validator.TestValidate(new CompleteInterviewCommand(Guid.Empty, "Feedback", 3, null));

        result.ShouldHaveValidationErrorFor(c => c.InterviewId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Should_Have_Error_When_Rating_Is_Out_Of_Range(int rating)
    {
        var result = _validator.TestValidate(new CompleteInterviewCommand(Guid.NewGuid(), "Feedback", rating, null));

        result.ShouldHaveValidationErrorFor(c => c.Rating);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CompleteInterviewCommand(Guid.NewGuid(), "Feedback", 5, null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
