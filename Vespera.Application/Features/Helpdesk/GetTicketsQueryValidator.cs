using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketsQueryValidator : AbstractValidator<GetTicketsQuery>
{
    public GetTicketsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThan(0);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 100);
    }
}
