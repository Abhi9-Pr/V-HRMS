using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class AcknowledgeAnnouncementCommandValidator : AbstractValidator<AcknowledgeAnnouncementCommand>
{
    public AcknowledgeAnnouncementCommandValidator()
    {
        RuleFor(command => command.AnnouncementId).NotEmpty();
    }
}
