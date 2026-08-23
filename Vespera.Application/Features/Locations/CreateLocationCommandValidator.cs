using FluentValidation;

namespace Vespera.Application.Features.Locations;

public sealed class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.AddressLine).MaximumLength(400);
        RuleFor(command => command.City).MaximumLength(100);
        RuleFor(command => command.Country).MaximumLength(100);
        RuleFor(command => command.TimeZoneId).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180);
    }
}
