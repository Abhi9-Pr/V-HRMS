using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class RetireAssetCommandValidator : AbstractValidator<RetireAssetCommand>
{
    public RetireAssetCommandValidator()
    {
        RuleFor(command => command.AssetId).NotEmpty();
    }
}
