using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class CompleteAssetRecoveryCommandValidator : AbstractValidator<CompleteAssetRecoveryCommand>
{
    public CompleteAssetRecoveryCommandValidator()
    {
        RuleFor(command => command.RecoveryId).NotEmpty();
    }
}
