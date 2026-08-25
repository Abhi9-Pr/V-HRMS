using FluentValidation;

namespace Vespera.Application.Features.Locations;

public sealed class GetLocationsQueryValidator : AbstractValidator<GetLocationsQuery>
{
    public GetLocationsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
