using FluentValidation;

namespace Vespera.Application.Features.Workspace;

public sealed class SetAnnouncementPinnedCommandValidator : AbstractValidator<SetAnnouncementPinnedCommand>
{
    public SetAnnouncementPinnedCommandValidator()
    {
        RuleFor(command => command.AnnouncementId).NotEmpty();
    }
}
