using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class RescheduleInterviewCommandValidatorTests
{
    private readonly RescheduleInterviewCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_InterviewId_Is_Empty()
    {
        var result = _validator.TestValidate(new RescheduleInterviewCommand(Guid.Empty, DateTimeOffset.UtcNow.AddDays(1), null));

        result.ShouldHaveValidationErrorFor(c => c.InterviewId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RescheduleInterviewCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
