using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class GetMyExpenseClaimsQueryValidator : AbstractValidator<GetMyExpenseClaimsQuery>
{
    public GetMyExpenseClaimsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
