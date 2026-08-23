using FluentValidation;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class RejectRegularizationCommandValidator : AbstractValidator<RejectRegularizationCommand>
{
    public RejectRegularizationCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(1000);
    }
}
