using FluentValidation;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class DeleteRotationPatternCommandValidator : AbstractValidator<DeleteRotationPatternCommand>
{
    public DeleteRotationPatternCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
