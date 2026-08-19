using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.DeviceId).NotEmpty().MaximumLength(256);
    }
}
