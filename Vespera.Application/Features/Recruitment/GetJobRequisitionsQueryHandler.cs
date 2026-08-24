using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetJobRequisitionsQueryHandler : IRequestHandler<GetJobRequisitionsQuery, Result<PagedResult<JobRequisitionDto>>>
{
    private readonly IReadRepository<JobRequisition> _requisitions;
    private readonly ITenantContext _tenantContext;

    public GetJobRequisitionsQueryHandler(IReadRepository<JobRequisition> requisitions, ITenantContext tenantContext)
    {
        _requisitions = requisitions;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<JobRequisitionDto>>> Handle(GetJobRequisitionsQuery request, CancellationToken cancellationToken)
    {
        var specification = new JobRequisitionsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var requisitions = await _requisitions.ListAsync(specification, cancellationToken);
        var totalCount = await _requisitions.CountAsync(specification, cancellationToken);

        var items = requisitions.Adapt<List<JobRequisitionDto>>();

        return Result.Success(new PagedResult<JobRequisitionDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
