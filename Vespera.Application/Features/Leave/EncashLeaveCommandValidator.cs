using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class EncashLeaveCommandValidator : AbstractValidator<EncashLeaveCommand>
{
    public EncashLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.Days).GreaterThan(0);
    }
}
