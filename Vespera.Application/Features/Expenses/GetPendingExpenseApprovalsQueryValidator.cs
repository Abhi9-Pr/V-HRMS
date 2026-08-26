using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class GetPendingExpenseApprovalsQueryValidator : AbstractValidator<GetPendingExpenseApprovalsQuery>
{
    public GetPendingExpenseApprovalsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
