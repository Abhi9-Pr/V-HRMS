using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(254);
    }
}
