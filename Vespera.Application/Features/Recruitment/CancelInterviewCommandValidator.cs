using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class CancelInterviewCommandValidator : AbstractValidator<CancelInterviewCommand>
{
    public CancelInterviewCommandValidator()
    {
        RuleFor(command => command.InterviewId).NotEmpty();
    }
}
