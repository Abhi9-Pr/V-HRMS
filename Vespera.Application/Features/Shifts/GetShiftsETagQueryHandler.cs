using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class GetShiftsETagQueryHandler : IRequestHandler<GetShiftsETagQuery, Result<string>>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;

    public GetShiftsETagQueryHandler(IReadRepository<Shift> shifts, ITenantContext tenantContext)
    {
        _shifts = shifts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetShiftsETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var shifts = await _shifts.ListAsync(new ShiftsByTenantSpecification(tenantId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            foreach (var shift in shifts.OrderBy(s => s.Id.Value))
            {
                writer.Write(shift.Id.Value.ToByteArray());
                writer.Write(shift.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
