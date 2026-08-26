using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetTicketCategoriesQueryHandler : IRequestHandler<GetTicketCategoriesQuery, Result<PagedResult<TicketCategoryDto>>>
{
    private readonly IReadRepository<TicketCategory> _categories;
    private readonly ITenantContext _tenantContext;

    public GetTicketCategoriesQueryHandler(IReadRepository<TicketCategory> categories, ITenantContext tenantContext)
    {
        _categories = categories;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<TicketCategoryDto>>> Handle(GetTicketCategoriesQuery request, CancellationToken cancellationToken)
    {
        var specification = new TicketCategoriesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var categories = await _categories.ListAsync(specification, cancellationToken);
        var totalCount = await _categories.CountAsync(specification, cancellationToken);

        var items = categories.Adapt<List<TicketCategoryDto>>();

        return Result.Success(new PagedResult<TicketCategoryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
