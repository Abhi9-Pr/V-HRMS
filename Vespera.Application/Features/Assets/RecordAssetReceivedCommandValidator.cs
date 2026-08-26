using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class RecordAssetReceivedCommandValidator : AbstractValidator<RecordAssetReceivedCommand>
{
    public RecordAssetReceivedCommandValidator()
    {
        RuleFor(command => command.RecoveryId).NotEmpty();
    }
}
