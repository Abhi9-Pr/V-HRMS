using FluentValidation;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class UpdateLeavePolicySettingsCommandValidator : AbstractValidator<UpdateLeavePolicySettingsCommand>
{
    public UpdateLeavePolicySettingsCommandValidator()
    {
        RuleFor(x => x.LeavePolicyId).NotEmpty();
        RuleFor(x => x.MinimumTenureMonthsForAccrual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxNegativeBalanceDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccrualFrequency).Must(s => Enum.TryParse<AccrualFrequency>(s, out _))
            .WithMessage($"AccrualFrequency must be one of: {string.Join(", ", Enum.GetNames<AccrualFrequency>())}.");
        RuleFor(x => x.NegativeBalancePolicy).Must(s => Enum.TryParse<NegativeBalancePolicy>(s, out _))
            .WithMessage($"NegativeBalancePolicy must be one of: {string.Join(", ", Enum.GetNames<NegativeBalancePolicy>())}.");
    }
}
