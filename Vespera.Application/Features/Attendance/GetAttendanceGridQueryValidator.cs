using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class GetAttendanceGridQueryValidator : AbstractValidator<GetAttendanceGridQuery>
{
    private const int MaxRangeDays = 31;

    public GetAttendanceGridQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);

        RuleFor(query => query.RangeEnd)
            .GreaterThanOrEqualTo(query => query.RangeStart)
            .WithMessage("rangeEnd must not be before rangeStart.");

        // A grid response is (employees on the page) x (days in range) — the employee side is
        // already page-bounded, so this is what keeps one page's response size bounded on the
        // date axis too, regardless of how wide a range a caller asks for.
        RuleFor(query => query)
            .Must(query => query.RangeEnd.DayNumber - query.RangeStart.DayNumber < MaxRangeDays)
            .WithMessage($"The date range cannot span more than {MaxRangeDays} days.")
            .OverridePropertyName("rangeEnd");
    }
}
