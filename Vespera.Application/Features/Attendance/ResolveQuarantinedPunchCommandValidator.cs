using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class ResolveQuarantinedPunchCommandValidator : AbstractValidator<ResolveQuarantinedPunchCommand>
{
    public ResolveQuarantinedPunchCommandValidator()
    {
        RuleFor(command => command.QuarantinedBiometricPunchId).NotEmpty();
        RuleFor(command => command.EmployeeId).NotEmpty();
    }
}
