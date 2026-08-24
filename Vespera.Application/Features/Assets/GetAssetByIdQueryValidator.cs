using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class GetAssetByIdQueryValidator : AbstractValidator<GetAssetByIdQuery>
{
    public GetAssetByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}
