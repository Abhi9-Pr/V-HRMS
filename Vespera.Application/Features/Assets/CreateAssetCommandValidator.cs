using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(command => command.AssetTag).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Category).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PurchaseCost).GreaterThanOrEqualTo(0);
        RuleFor(command => command.PurchaseCostCurrency).IsInEnum();
        RuleFor(command => command.SerialNumber).MaximumLength(128);
        RuleFor(command => command.MacAddress).MaximumLength(128);
    }
}
