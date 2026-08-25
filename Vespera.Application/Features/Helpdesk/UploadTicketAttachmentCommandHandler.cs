using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class UploadTicketAttachmentCommandHandler : IRequestHandler<UploadTicketAttachmentCommand, Result<string>>
{
    private readonly IReadRepository<Ticket> _tickets;
    private readonly IFileStorage _fileStorage;

    public UploadTicketAttachmentCommandHandler(IReadRepository<Ticket> tickets, IFileStorage fileStorage)
    {
        _tickets = tickets;
        _fileStorage = fileStorage;
    }

    public async Task<Result<string>> Handle(UploadTicketAttachmentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.FirstOrDefaultAsync(new TicketByIdSpecification(new TicketId(request.TicketId)), cancellationToken);
        if (ticket is null)
        {
            return Result.Failure<string>(Error.NotFound("ticket.not_found", "Ticket not found."));
        }

        var reference = await _fileStorage.UploadAsync(request.FileName, new MemoryStream(request.Content), cancellationToken);
        return Result.Success(reference);
    }
}
