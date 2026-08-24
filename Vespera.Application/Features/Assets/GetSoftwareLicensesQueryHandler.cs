using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class GetSoftwareLicensesQueryHandler : IRequestHandler<GetSoftwareLicensesQuery, Result<PagedResult<SoftwareLicenseDto>>>
{
    private readonly IReadRepository<SoftwareLicense> _licenses;
    private readonly ITenantContext _tenantContext;

    public GetSoftwareLicensesQueryHandler(IReadRepository<SoftwareLicense> licenses, ITenantContext tenantContext)
    {
        _licenses = licenses;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<SoftwareLicenseDto>>> Handle(GetSoftwareLicensesQuery request, CancellationToken cancellationToken)
    {
        var specification = new SoftwareLicensesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var licenses = await _licenses.ListAsync(specification, cancellationToken);
        var totalCount = await _licenses.CountAsync(specification, cancellationToken);

        var items = licenses.Adapt<List<SoftwareLicenseDto>>();

        return Result.Success(new PagedResult<SoftwareLicenseDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
