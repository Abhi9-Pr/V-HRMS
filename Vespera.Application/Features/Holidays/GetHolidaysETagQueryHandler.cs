using System.Security.Cryptography;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Holidays;

public sealed class GetHolidaysETagQueryHandler : IRequestHandler<GetHolidaysETagQuery, Result<string>>
{
    private readonly IReadRepository<Holiday> _holidays;
    private readonly ITenantContext _tenantContext;

    public GetHolidaysETagQueryHandler(IReadRepository<Holiday> holidays, ITenantContext tenantContext)
    {
        _holidays = holidays;
        _tenantContext = tenantContext;
    }

    public async Task<Result<string>> Handle(GetHolidaysETagQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var locationId = request.LocationId is { } id ? new LocationId(id) : (LocationId?)null;
        var holidays = await _holidays.ListAsync(
            new HolidaysByTenantAndLocationSpecification(tenantId, locationId), cancellationToken);

        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(tenantId.Value.ToByteArray());
            writer.Write(locationId?.Value.ToByteArray() ?? []);
            foreach (var holiday in holidays.OrderBy(h => h.Id.Value))
            {
                writer.Write(holiday.Id.Value.ToByteArray());
                writer.Write(holiday.RowVersion);
            }
        }

        return Result.Success(Convert.ToHexString(SHA256.HashData(buffer.ToArray())));
    }
}
