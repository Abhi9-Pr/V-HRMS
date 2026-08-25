using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PagedResult<TicketSummaryDto>>>
{
    private readonly IReadRepository<Ticket> _tickets;
    private readonly ITenantContext _tenantContext;

    public GetTicketsQueryHandler(IReadRepository<Ticket> tickets, ITenantContext tenantContext)
    {
        _tickets = tickets;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<TicketSummaryDto>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        var specification = new TicketsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var tickets = await _tickets.ListAsync(specification, cancellationToken);
        var totalCount = await _tickets.CountAsync(specification, cancellationToken);

        var items = tickets.Adapt<List<TicketSummaryDto>>();

        return Result.Success(new PagedResult<TicketSummaryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
