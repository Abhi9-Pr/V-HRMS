using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Tenants;

public sealed class GetTenantIdByCodeQueryHandler : IRequestHandler<GetTenantIdByCodeQuery, Result<TenantLookupDto>>
{
    private readonly IReadRepository<Tenant> _tenants;

    public GetTenantIdByCodeQueryHandler(IReadRepository<Tenant> tenants)
    {
        _tenants = tenants;
    }

    public async Task<Result<TenantLookupDto>> Handle(GetTenantIdByCodeQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenants.FirstOrDefaultAsync(new TenantByCodeSpecification(request.Code), cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantLookupDto>(Error.NotFound("tenant.not_found", "No tenant matches that code."));
        }

        return Result.Success(new TenantLookupDto(tenant.Id.Value, tenant.Name));
    }
}
