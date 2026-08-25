using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class WriteOffAssetCommandValidator : AbstractValidator<WriteOffAssetCommand>
{
    public WriteOffAssetCommandValidator()
    {
        RuleFor(command => command.RecoveryId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Currency).IsInEnum();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(1024);
    }
}
