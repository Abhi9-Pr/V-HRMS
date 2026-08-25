using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class GetQuarantinedPunchesQueryValidator : AbstractValidator<GetQuarantinedPunchesQuery>
{
    public GetQuarantinedPunchesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
