using FluentValidation;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Leave;

public sealed class UpdateLeaveTypeEligibilityCommandValidator : AbstractValidator<UpdateLeaveTypeEligibilityCommand>
{
    public UpdateLeaveTypeEligibilityCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.MinimumTenureMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxEncashableDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ApplicableGender).Must(g => g is null || Enum.TryParse<Gender>(g, out _))
            .WithMessage($"ApplicableGender must be null or one of: {string.Join(", ", Enum.GetNames<Gender>())}.");
    }
}
