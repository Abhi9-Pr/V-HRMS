using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class RejectCandidateCommandValidator : AbstractValidator<RejectCandidateCommand>
{
    public RejectCandidateCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
    }
}
