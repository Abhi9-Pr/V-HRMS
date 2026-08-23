using FluentValidation;

namespace Vespera.Application.Features.Shifts;

public sealed class GetShiftsQueryValidator : AbstractValidator<GetShiftsQuery>
{
    public GetShiftsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
