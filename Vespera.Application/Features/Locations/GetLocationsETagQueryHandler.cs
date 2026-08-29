using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class GetLocationsETagQueryHandler : IRequestHandler<GetLocationsETagQuery, Result<string>>
{
    private readonly IReadRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;

    public GetLocationsETagQueryHandler(IReadRepository<Location> locations, ITenantContext tenantContext)
    {
        _locations = locations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetLocationsETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var locations = await _locations.ListAsync(new LocationsByTenantSpecification(tenantId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            foreach (var location in locations.OrderBy(l => l.Id.Value))
            {
                writer.Write(location.Id.Value.ToByteArray());
                writer.Write(location.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
