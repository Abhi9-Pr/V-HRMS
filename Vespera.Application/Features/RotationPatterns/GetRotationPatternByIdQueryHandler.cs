using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class GetRotationPatternByIdQueryHandler : IRequestHandler<GetRotationPatternByIdQuery, Result<RotationPatternDto>>
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns;
    private readonly ITenantContext _tenantContext;

    public GetRotationPatternByIdQueryHandler(IReadRepository<RotationPattern> rotationPatterns, ITenantContext tenantContext)
    {
        _rotationPatterns = rotationPatterns;
        _tenantContext = tenantContext;
    }

    public async Task<Result<RotationPatternDto>> Handle(GetRotationPatternByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new RotationPatternByIdSpecification(_tenantContext.TenantId, new RotationPatternId(request.Id));

        var rotationPattern = await _rotationPatterns.FirstOrDefaultAsync(specification, cancellationToken);
        if (rotationPattern is null)
        {
            return Result.Failure<RotationPatternDto>(Error.NotFound("rotation_pattern.not_found", "Rotation pattern not found."));
        }

        return Result.Success(rotationPattern.Adapt<RotationPatternDto>());
    }
}
