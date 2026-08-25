using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record GetQuarantinedPunchesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<QuarantinedBiometricPunchDto>>>;

public sealed record QuarantinedBiometricPunchDto(
    Guid Id, Guid BiometricDeviceId, string DeviceUserId, DateTimeOffset PunchedAtUtc, string? PunchType, string Status);
