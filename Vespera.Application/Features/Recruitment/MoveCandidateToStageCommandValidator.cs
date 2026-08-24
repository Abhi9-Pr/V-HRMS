using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class MoveCandidateToStageCommandValidator : AbstractValidator<MoveCandidateToStageCommand>
{
    public MoveCandidateToStageCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
        RuleFor(command => command.TargetStageId).NotEmpty();
    }
}
