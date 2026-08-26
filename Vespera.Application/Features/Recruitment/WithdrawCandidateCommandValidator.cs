using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class WithdrawCandidateCommandValidator : AbstractValidator<WithdrawCandidateCommand>
{
    public WithdrawCandidateCommandValidator()
    {
        RuleFor(command => command.CandidateId).NotEmpty();
    }
}
