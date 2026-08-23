using FluentValidation;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class SubmitRegularizationCommandValidator : AbstractValidator<SubmitRegularizationCommand>
{
    public SubmitRegularizationCommandValidator()
    {
        RuleFor(x => x.AttendanceDayId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EvidenceFileName).NotEmpty().When(x => x.EvidenceContent is not null);
    }
}
