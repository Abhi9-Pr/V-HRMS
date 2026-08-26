using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class CancelApprovedLeaveRequestCommandValidator : AbstractValidator<CancelApprovedLeaveRequestCommand>
{
    public CancelApprovedLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
