using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class ScheduleInterviewCommandValidatorTests
{
    private readonly ScheduleInterviewCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CandidateId_Is_Empty()
    {
        var command = new ScheduleInterviewCommand(Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), [Guid.NewGuid()], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CandidateId);
    }

    [Fact]
    public void Should_Have_Error_When_PipelineStageId_Is_Empty()
    {
        var command = new ScheduleInterviewCommand(Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow.AddDays(1), [Guid.NewGuid()], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PipelineStageId);
    }

    [Fact]
    public void Should_Have_Error_When_InterviewerIds_Is_Empty()
    {
        var command = new ScheduleInterviewCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), [], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.InterviewerIds);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ScheduleInterviewCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), [Guid.NewGuid()], null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
