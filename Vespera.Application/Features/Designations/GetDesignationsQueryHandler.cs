using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class GetDesignationsQueryHandler : IRequestHandler<GetDesignationsQuery, Result<PagedResult<DesignationDto>>>
{
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;

    public GetDesignationsQueryHandler(IReadRepository<Designation> designations, ITenantContext tenantContext)
    {
        _designations = designations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<DesignationDto>>> Handle(GetDesignationsQuery request, CancellationToken cancellationToken)
    {
        var specification = new DesignationsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var designations = await _designations.ListAsync(specification, cancellationToken);
        var totalCount = await _designations.CountAsync(specification, cancellationToken);

        var items = designations.Adapt<List<DesignationDto>>();

        return Result.Success(new PagedResult<DesignationDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
