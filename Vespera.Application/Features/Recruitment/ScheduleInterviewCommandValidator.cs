using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
        RuleFor(command => command.PipelineStageId).NotEmpty();
        RuleFor(command => command.InterviewerIds).NotEmpty();
    }
}
