using FluentValidation;

namespace Vespera.Application.Features.Locations;

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AddressLine).MaximumLength(400);
        RuleFor(command => command.City).MaximumLength(100);
        RuleFor(command => command.Country).MaximumLength(100);
        RuleFor(command => command.Latitude).InclusiveBetween(-90, 90);
        RuleFor(command => command.Longitude).InclusiveBetween(-180, 180);
    }
}
