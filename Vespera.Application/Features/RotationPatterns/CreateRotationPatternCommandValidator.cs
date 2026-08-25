using FluentValidation;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class CreateRotationPatternCommandValidator : AbstractValidator<CreateRotationPatternCommand>
{
    public CreateRotationPatternCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Days).NotEmpty();
    }
}
