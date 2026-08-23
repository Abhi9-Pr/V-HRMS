using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class DeleteBlackoutPeriodCommandValidator : AbstractValidator<DeleteBlackoutPeriodCommand>
{
    public DeleteBlackoutPeriodCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
