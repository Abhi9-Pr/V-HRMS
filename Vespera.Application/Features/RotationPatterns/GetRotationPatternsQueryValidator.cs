using FluentValidation;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class GetRotationPatternsQueryValidator : AbstractValidator<GetRotationPatternsQuery>
{
    public GetRotationPatternsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
