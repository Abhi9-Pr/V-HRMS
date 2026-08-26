using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class CreateSoftwareLicenseCommandValidator : AbstractValidator<CreateSoftwareLicenseCommand>
{
    public CreateSoftwareLicenseCommandValidator()
    {
        RuleFor(command => command.ProductName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.SeatCount).GreaterThan(0);
    }
}
