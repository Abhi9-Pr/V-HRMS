using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateCorporateEventCommandValidator : AbstractValidator<CreateCorporateEventCommand>
{
    public CreateCorporateEventCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Description).NotEmpty();
        RuleFor(command => command.LocationText).NotEmpty().MaximumLength(256);
        RuleFor(command => command.EndsAt).GreaterThan(command => command.StartsAt);
    }
}
