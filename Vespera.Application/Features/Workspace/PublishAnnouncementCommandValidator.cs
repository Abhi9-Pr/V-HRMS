using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class PublishAnnouncementCommandValidator : AbstractValidator<PublishAnnouncementCommand>
{
    public PublishAnnouncementCommandValidator()
    {
        RuleFor(command => command.AnnouncementId).NotEmpty();
    }
}
