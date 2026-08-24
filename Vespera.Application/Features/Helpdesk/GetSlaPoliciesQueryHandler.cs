using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class GetSlaPoliciesQueryHandler : IRequestHandler<GetSlaPoliciesQuery, Result<PagedResult<SlaPolicyDto>>>
{
    private readonly IReadRepository<SlaPolicy> _policies;
    private readonly ITenantContext _tenantContext;

    public GetSlaPoliciesQueryHandler(IReadRepository<SlaPolicy> policies, ITenantContext tenantContext)
    {
        _policies = policies;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<SlaPolicyDto>>> Handle(GetSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var specification = new SlaPoliciesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var policies = await _policies.ListAsync(specification, cancellationToken);
        var totalCount = await _policies.CountAsync(specification, cancellationToken);

        var items = policies.Adapt<List<SlaPolicyDto>>();

        return Result.Success(new PagedResult<SlaPolicyDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
