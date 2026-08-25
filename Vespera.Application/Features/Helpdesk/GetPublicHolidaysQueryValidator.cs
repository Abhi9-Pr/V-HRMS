using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetPublicHolidaysQueryValidator : AbstractValidator<GetPublicHolidaysQuery>
{
    public GetPublicHolidaysQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThan(0);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 100);
    }
}
