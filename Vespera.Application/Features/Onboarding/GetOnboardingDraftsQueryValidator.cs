using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class GetOnboardingDraftsQueryValidator : AbstractValidator<GetOnboardingDraftsQuery>
{
    public GetOnboardingDraftsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
