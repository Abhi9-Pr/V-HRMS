using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class RescheduleInterviewCommandValidator : AbstractValidator<RescheduleInterviewCommand>
{
    public RescheduleInterviewCommandValidator()
    {
        RuleFor(command => command.InterviewId).NotEmpty();
    }
}
