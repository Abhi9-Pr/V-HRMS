using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class CreateBlackoutPeriodCommandValidator : AbstractValidator<CreateBlackoutPeriodCommand>
{
    public CreateBlackoutPeriodCommandValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
