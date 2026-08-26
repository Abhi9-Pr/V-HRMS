using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class GetPendingAssetRecoveriesQueryValidator : AbstractValidator<GetPendingAssetRecoveriesQuery>
{
    public GetPendingAssetRecoveriesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
