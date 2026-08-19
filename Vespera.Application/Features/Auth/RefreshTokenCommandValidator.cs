using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
        RuleFor(command => command.DeviceId).NotEmpty().MaximumLength(256);
    }
}
