using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class SubmitInterviewScorecardCommandValidator : AbstractValidator<SubmitInterviewScorecardCommand>
{
    public SubmitInterviewScorecardCommandValidator()
    {
        RuleFor(command => command.InterviewId).NotEmpty();
        RuleFor(command => command.InterviewerId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
    }
}
