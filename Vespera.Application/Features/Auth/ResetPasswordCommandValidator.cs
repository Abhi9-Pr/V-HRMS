using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
        RuleFor(command => command.ResetToken).NotEmpty();
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(12);
    }
}
