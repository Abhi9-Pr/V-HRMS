using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed class GetBiometricDevicesQueryHandler : IRequestHandler<GetBiometricDevicesQuery, Result<PagedResult<BiometricDeviceDto>>>
{
    private readonly IReadRepository<BiometricDevice> _devices;
    private readonly ITenantContext _tenantContext;

    public GetBiometricDevicesQueryHandler(IReadRepository<BiometricDevice> devices, ITenantContext tenantContext)
    {
        _devices = devices;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<BiometricDeviceDto>>> Handle(GetBiometricDevicesQuery request, CancellationToken cancellationToken)
    {
        var specification = new BiometricDevicesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var devices = await _devices.ListAsync(specification, cancellationToken);
        var totalCount = await _devices.CountAsync(specification, cancellationToken);

        var items = devices.Adapt<List<BiometricDeviceDto>>();

        return Result.Success(new PagedResult<BiometricDeviceDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
