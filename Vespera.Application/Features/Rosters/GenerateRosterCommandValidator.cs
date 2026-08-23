using FluentValidation;

namespace Vespera.Application.Features.Rosters;

public sealed class GenerateRosterCommandValidator : AbstractValidator<GenerateRosterCommand>
{
    private const int MaxRangeDays = 366;

    public GenerateRosterCommandValidator()
    {
        RuleFor(command => command.RotationPatternId).NotEmpty();
        RuleFor(command => command.EmployeeIds).NotEmpty();
        RuleFor(command => command.RangeEnd).GreaterThanOrEqualTo(command => command.RangeStart);
        RuleFor(command => command)
            .Must(command => command.RangeEnd.DayNumber - command.RangeStart.DayNumber < MaxRangeDays)
            .WithMessage($"A roster can be generated for at most {MaxRangeDays} days at a time.");
    }
}
