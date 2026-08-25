using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class GetDesignationByIdQueryHandler : IRequestHandler<GetDesignationByIdQuery, Result<DesignationDto>>
{
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;

    public GetDesignationByIdQueryHandler(IReadRepository<Designation> designations, ITenantContext tenantContext)
    {
        _designations = designations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<DesignationDto>> Handle(GetDesignationByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new DesignationByIdSpecification(_tenantContext.TenantId, new DesignationId(request.Id));

        var designation = await _designations.FirstOrDefaultAsync(specification, cancellationToken);
        if (designation is null)
        {
            return Result.Failure<DesignationDto>(Error.NotFound("designation.not_found", "Designation not found."));
        }

        return Result.Success(designation.Adapt<DesignationDto>());
    }
}
