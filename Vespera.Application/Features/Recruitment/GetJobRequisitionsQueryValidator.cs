using FluentValidation;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetJobRequisitionsQueryValidator : AbstractValidator<GetJobRequisitionsQuery>
{
    public GetJobRequisitionsQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThan(0);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
