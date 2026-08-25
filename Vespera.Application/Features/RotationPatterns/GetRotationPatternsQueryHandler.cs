using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class GetRotationPatternsQueryHandler : IRequestHandler<GetRotationPatternsQuery, Result<PagedResult<RotationPatternDto>>>
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns;
    private readonly ITenantContext _tenantContext;

    public GetRotationPatternsQueryHandler(IReadRepository<RotationPattern> rotationPatterns, ITenantContext tenantContext)
    {
        _rotationPatterns = rotationPatterns;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<RotationPatternDto>>> Handle(GetRotationPatternsQuery request, CancellationToken cancellationToken)
    {
        var specification = new RotationPatternsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var rotationPatterns = await _rotationPatterns.ListAsync(specification, cancellationToken);
        var totalCount = await _rotationPatterns.CountAsync(specification, cancellationToken);

        var items = rotationPatterns.Adapt<List<RotationPatternDto>>();

        return Result.Success(new PagedResult<RotationPatternDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
