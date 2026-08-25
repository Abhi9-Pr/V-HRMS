using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class CompleteInterviewCommandValidator : AbstractValidator<CompleteInterviewCommand>
{
    public CompleteInterviewCommandValidator()
    {
        RuleFor(command => command.InterviewId).NotEmpty();
        RuleFor(command => command.Rating).InclusiveBetween(1, 5);
    }
}
