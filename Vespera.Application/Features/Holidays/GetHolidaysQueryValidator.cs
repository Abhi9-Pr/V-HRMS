using FluentValidation;

namespace Vespera.Application.Features.Holidays;

public sealed class GetHolidaysQueryValidator : AbstractValidator<GetHolidaysQuery>
{
    public GetHolidaysQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
