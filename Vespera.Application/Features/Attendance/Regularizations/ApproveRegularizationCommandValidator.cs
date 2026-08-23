using FluentValidation;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class ApproveRegularizationCommandValidator : AbstractValidator<ApproveRegularizationCommand>
{
    public ApproveRegularizationCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}
