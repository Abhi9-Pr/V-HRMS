using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record GetBiometricDevicesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<BiometricDeviceDto>>>;

public sealed record BiometricDeviceDto(
    Guid Id, Guid LocationId, string VendorType, string Host, int Port, string? Cursor, bool IsActive);
