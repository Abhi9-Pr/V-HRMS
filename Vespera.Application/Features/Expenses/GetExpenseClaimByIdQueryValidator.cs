using FluentValidation;

namespace Vespera.Application.Features.Expenses;

public sealed class GetExpenseClaimByIdQueryValidator : AbstractValidator<GetExpenseClaimByIdQuery>
{
    public GetExpenseClaimByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
