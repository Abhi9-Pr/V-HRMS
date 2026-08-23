using FluentValidation;

namespace Vespera.Application.Features.Rosters;

public sealed class GetRosterQueryValidator : AbstractValidator<GetRosterQuery>
{
    private const int MaxRangeDays = 366;

    public GetRosterQueryValidator()
    {
        RuleFor(query => query.RangeEnd).GreaterThanOrEqualTo(query => query.RangeStart);
        RuleFor(query => query)
            .Must(query => query.RangeEnd.DayNumber - query.RangeStart.DayNumber < MaxRangeDays)
            .WithMessage($"A roster can be queried for at most {MaxRangeDays} days at a time.");
    }
}
