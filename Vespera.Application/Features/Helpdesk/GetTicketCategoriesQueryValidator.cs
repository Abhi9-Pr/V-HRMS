using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketCategoriesQueryValidator : AbstractValidator<GetTicketCategoriesQuery>
{
    public GetTicketCategoriesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThan(0);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 100);
    }
}
