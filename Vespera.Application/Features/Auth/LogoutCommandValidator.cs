using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}
