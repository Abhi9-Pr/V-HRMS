using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class GetSoftwareLicensesQueryValidator : AbstractValidator<GetSoftwareLicensesQuery>
{
    public GetSoftwareLicensesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
