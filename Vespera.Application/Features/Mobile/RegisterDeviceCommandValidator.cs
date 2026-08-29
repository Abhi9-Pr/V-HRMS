using FluentValidation;

namespace Vespera.Application.Features.Mobile;

public sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(command => command.DeviceId).NotEmpty().MaximumLength(256);
        RuleFor(command => command.PushToken).NotEmpty().MaximumLength(1024);
        RuleFor(command => command.Platform).IsInEnum();
    }
}
