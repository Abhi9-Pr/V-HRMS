using FluentValidation;

namespace Vespera.Application.Features.Auth;

public sealed class ConfirmTotpEnrollmentCommandValidator : AbstractValidator<ConfirmTotpEnrollmentCommand>
{
    public ConfirmTotpEnrollmentCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().Length(6);
    }
}
