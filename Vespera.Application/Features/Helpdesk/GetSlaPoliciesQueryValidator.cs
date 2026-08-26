using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetSlaPoliciesQueryValidator : AbstractValidator<GetSlaPoliciesQuery>
{
    public GetSlaPoliciesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThan(0);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 100);
    }
}
