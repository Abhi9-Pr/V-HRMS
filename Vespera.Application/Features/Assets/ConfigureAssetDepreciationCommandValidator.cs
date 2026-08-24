using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class ConfigureAssetDepreciationCommandValidator : AbstractValidator<ConfigureAssetDepreciationCommand>
{
    public ConfigureAssetDepreciationCommandValidator()
    {
        RuleFor(command => command.Method).IsInEnum();
        RuleFor(command => command.UsefulLifeMonths).GreaterThan(0);
        RuleFor(command => command.SalvageValue).GreaterThanOrEqualTo(0);
        RuleFor(command => command.SalvageValueCurrency).IsInEnum();
    }
}
