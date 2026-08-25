using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class RegisterBiometricDeviceCommandValidator : AbstractValidator<RegisterBiometricDeviceCommand>
{
    public RegisterBiometricDeviceCommandValidator()
    {
        RuleFor(command => command.LocationId).NotEmpty();
        RuleFor(command => command.VendorType).NotEmpty().Must(v => Enum.TryParse<Domain.Attendance.BiometricVendorType>(v, ignoreCase: true, out _))
            .WithMessage("VendorType must be a supported biometric vendor.");
        RuleFor(command => command.Host).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Port).InclusiveBetween(1, 65535);
    }
}
