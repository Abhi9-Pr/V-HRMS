using FluentValidation;

namespace Vespera.Application.Features.Attendance;

public sealed class GetBiometricDevicesQueryValidator : AbstractValidator<GetBiometricDevicesQuery>
{
    public GetBiometricDevicesQueryValidator()
    {
        RuleFor(query => query.Paging.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Paging.PageSize).InclusiveBetween(1, 200);
    }
}
