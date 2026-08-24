using Mapster;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class TicketMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ticket, TicketDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.RaisedBy, src => src.RaisedBy.Value)
            .Map(dest => dest.AssignedTo, src => src.AssignedTo != null ? src.AssignedTo.Value.Value : (Guid?)null)
            .Map(dest => dest.Comments, src => src.Comments);

        config.NewConfig<Ticket, TicketSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.AssignedTo, src => src.AssignedTo != null ? src.AssignedTo.Value.Value : (Guid?)null);

        config.NewConfig<TicketComment, TicketCommentDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.AuthorId, src => src.AuthorId.Value)
            .Map(dest => dest.ParentCommentId, src => src.ParentCommentId != null ? src.ParentCommentId.Value.Value : (Guid?)null);
    }
}
