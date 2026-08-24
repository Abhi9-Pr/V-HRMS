using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class GetUnusedSeatsReportQueryHandler : IRequestHandler<GetUnusedSeatsReportQuery, Result<IReadOnlyList<UnusedSeatsReportRowDto>>>
{
    private readonly IReadRepository<SoftwareLicense> _licenses;
    private readonly ITenantContext _tenantContext;

    public GetUnusedSeatsReportQueryHandler(IReadRepository<SoftwareLicense> licenses, ITenantContext tenantContext)
    {
        _licenses = licenses;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<UnusedSeatsReportRowDto>>> Handle(
        GetUnusedSeatsReportQuery request, CancellationToken cancellationToken)
    {
        var licenses = await _licenses.ListAsync(new AllSoftwareLicensesSpecification(_tenantContext.TenantId), cancellationToken);

        IReadOnlyList<UnusedSeatsReportRowDto> rows = licenses
            .Select(license => new UnusedSeatsReportRowDto(
                license.Id.Value, license.ProductName, license.SeatCount, license.SeatsUsed,
                license.SeatCount - license.SeatsUsed, license.ExpiresAt))
            .ToList();

        return Result.Success(rows);
    }
}
