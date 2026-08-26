using FluentValidation;

namespace Vespera.Application.Features.Helpdesk;

public sealed class UploadTicketAttachmentCommandValidator : AbstractValidator<UploadTicketAttachmentCommand>
{
    public UploadTicketAttachmentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Content).NotEmpty();
    }
}
