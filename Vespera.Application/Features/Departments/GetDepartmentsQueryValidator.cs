using FluentValidation;

namespace Vespera.Application.Features.Departments;

public sealed class GetDepartmentsQueryValidator : AbstractValidator<GetDepartmentsQuery>
{
    public GetDepartmentsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
