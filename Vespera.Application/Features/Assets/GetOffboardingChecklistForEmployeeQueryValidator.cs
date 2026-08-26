using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class GetOffboardingChecklistForEmployeeQueryValidator : AbstractValidator<GetOffboardingChecklistForEmployeeQuery>
{
    public GetOffboardingChecklistForEmployeeQueryValidator()
    {
        RuleFor(query => query.EmployeeId).NotEmpty();
    }
}
