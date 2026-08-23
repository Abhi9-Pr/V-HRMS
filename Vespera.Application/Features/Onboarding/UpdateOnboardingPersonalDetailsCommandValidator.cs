using FluentValidation;

namespace Vespera.Application.Features.Onboarding;

public sealed class UpdateOnboardingPersonalDetailsCommandValidator : AbstractValidator<UpdateOnboardingPersonalDetailsCommand>
{
    public UpdateOnboardingPersonalDetailsCommandValidator()
    {
        RuleFor(command => command.OnboardingDraftId).NotEmpty();
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.WorkEmail).NotEmpty().MaximumLength(254);
        RuleFor(command => command.Phone).NotEmpty();
        RuleFor(command => command.DateOfBirth).LessThan(command => DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
