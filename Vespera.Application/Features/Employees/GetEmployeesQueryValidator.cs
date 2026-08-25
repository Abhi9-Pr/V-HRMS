using FluentValidation;

namespace Vespera.Application.Features.Employees;

public sealed class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
