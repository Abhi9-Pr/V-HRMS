using FluentValidation;

namespace Vespera.Application.Features.Designations;

public sealed class CreateDesignationCommandValidator : AbstractValidator<CreateDesignationCommand>
{
    public CreateDesignationCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Grade).GreaterThan(0);
    }
}
