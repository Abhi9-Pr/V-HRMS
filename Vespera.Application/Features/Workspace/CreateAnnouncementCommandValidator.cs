using FluentValidation;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Body).NotEmpty();
        RuleFor(command => command.ExpiresAt)
            .GreaterThan(command => command.PublishAt)
            .When(command => command.ExpiresAt is not null);

        RuleFor(command => command.TargetDepartmentId)
            .NotNull()
            .When(command => command.AudienceScope == AnnouncementAudienceScope.Department)
            .WithMessage("A target department is required for a department-scoped announcement.");

        RuleFor(command => command.TargetLocationId)
            .NotNull()
            .When(command => command.AudienceScope == AnnouncementAudienceScope.Location)
            .WithMessage("A target location is required for a location-scoped announcement.");
    }
}
