using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class CreateLeavePolicyCommandValidator : AbstractValidator<CreateLeavePolicyCommand>
{
    public CreateLeavePolicyCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.AnnualEntitlementDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccrualRatePerMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxCarryForwardDays).GreaterThanOrEqualTo(0);
    }
}
