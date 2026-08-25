using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class MarkAssetUnderRepairCommandValidator : AbstractValidator<MarkAssetUnderRepairCommand>
{
    public MarkAssetUnderRepairCommandValidator()
    {
        RuleFor(command => command.AssetId).NotEmpty();
    }
}
