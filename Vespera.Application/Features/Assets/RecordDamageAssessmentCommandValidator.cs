using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class RecordDamageAssessmentCommandValidator : AbstractValidator<RecordDamageAssessmentCommand>
{
    public RecordDamageAssessmentCommandValidator()
    {
        RuleFor(command => command.RecoveryId).NotEmpty();
        RuleFor(command => command.Notes).NotEmpty().MaximumLength(1024);
    }
}
