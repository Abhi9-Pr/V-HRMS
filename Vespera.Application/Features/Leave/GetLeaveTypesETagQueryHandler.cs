using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetLeaveTypesETagQueryHandler : IRequestHandler<GetLeaveTypesETagQuery, Result<string>>
{
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly ITenantContext _tenantContext;

    public GetLeaveTypesETagQueryHandler(IReadRepository<LeaveType> leaveTypes, ITenantContext tenantContext)
    {
        _leaveTypes = leaveTypes;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetLeaveTypesETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var leaveTypes = await _leaveTypes.ListAsync(new LeaveTypesByTenantSpecification(tenantId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            foreach (var leaveType in leaveTypes.OrderBy(t => t.Id.Value))
            {
                writer.Write(leaveType.Id.Value.ToByteArray());
                writer.Write(leaveType.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
