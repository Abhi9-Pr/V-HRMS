using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class WithdrawLeaveRequestCommandValidator : AbstractValidator<WithdrawLeaveRequestCommand>
{
    public WithdrawLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveRequestId).NotEmpty();
    }
}
