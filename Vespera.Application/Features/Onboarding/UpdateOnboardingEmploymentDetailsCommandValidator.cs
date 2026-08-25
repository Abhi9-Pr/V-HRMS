using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class UpdateOnboardingEmploymentDetailsCommandValidator : AbstractValidator<UpdateOnboardingEmploymentDetailsCommand>
{
    public UpdateOnboardingEmploymentDetailsCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.DepartmentId).NotEmpty();
        RuleFor(command => command.DesignationId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
