using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class RecordWebPunchCommandValidator : AbstractValidator<RecordWebPunchCommand>
{
    public RecordWebPunchCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PunchType).NotEmpty().Must(v => Enum.TryParse<Domain.Attendance.PunchType>(v, ignoreCase: true, out _))
            .WithMessage("PunchType must be 'In' or 'Out'.");

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage("Latitude and Longitude must both be supplied, or neither.")
            .OverridePropertyName("Location");
    }
}
