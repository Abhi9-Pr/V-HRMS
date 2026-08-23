using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class RecomputeAttendanceDayCommandValidator : AbstractValidator<RecomputeAttendanceDayCommand>
{
    public RecomputeAttendanceDayCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.RangeEnd).GreaterThanOrEqualTo(x => x.RangeStart)
            .WithMessage("RangeEnd cannot be before RangeStart.");
    }
}
