using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class RecordMobilePunchCommandValidator : AbstractValidator<RecordMobilePunchCommand>
{
    public RecordMobilePunchCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.PunchType).NotEmpty().Must(v => Enum.TryParse<Domain.Attendance.PunchType>(v, ignoreCase: true, out _))
            .WithMessage("PunchType must be 'In' or 'Out'.");
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.Accuracy).GreaterThanOrEqualTo(0);

        // Required, not merely accepted — a mobile punch with no idempotency key can't be safely
        // retried on a flaky connection, which is exactly the scenario this endpoint exists for.
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Idempotency-Key is required for mobile punches.");

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithMessage("Latitude and Longitude must both be supplied, or neither.")
            .OverridePropertyName("Location");
    }
}
