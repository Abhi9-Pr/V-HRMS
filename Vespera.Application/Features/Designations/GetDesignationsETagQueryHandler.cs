using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class GetDesignationsETagQueryHandler : IRequestHandler<GetDesignationsETagQuery, Result<string>>
{
    private readonly IReadRepository<Designation> _designations;
    private readonly ITenantContext _tenantContext;

    public GetDesignationsETagQueryHandler(IReadRepository<Designation> designations, ITenantContext tenantContext)
    {
        _designations = designations;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetDesignationsETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var designations = await _designations.ListAsync(new DesignationsByTenantSpecification(tenantId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            foreach (var designation in designations.OrderBy(d => d.Id.Value))
            {
                writer.Write(designation.Id.Value.ToByteArray());
                writer.Write(designation.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
