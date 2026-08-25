using FluentValidation;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class UpdateRotationPatternCommandValidator : AbstractValidator<UpdateRotationPatternCommand>
{
    public UpdateRotationPatternCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Days).NotEmpty();
    }
}
