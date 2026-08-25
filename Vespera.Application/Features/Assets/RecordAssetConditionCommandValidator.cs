using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class RecordAssetConditionCommandValidator : AbstractValidator<RecordAssetConditionCommand>
{
    public RecordAssetConditionCommandValidator()
    {
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.Rating).IsInEnum();
        RuleFor(command => command.Notes).MaximumLength(1024);
    }
}
