using FluentValidation;

namespace Vespera.Application.Features.Designations;

public sealed class GetDesignationsQueryValidator : AbstractValidator<GetDesignationsQuery>
{
    public GetDesignationsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
