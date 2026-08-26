using FluentValidation;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class GetExpensePoliciesQueryValidator : AbstractValidator<GetExpensePoliciesQuery>
{
    public GetExpensePoliciesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
