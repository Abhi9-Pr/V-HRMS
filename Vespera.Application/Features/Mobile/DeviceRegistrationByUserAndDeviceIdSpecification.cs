using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

/// <summary>Finds the caller's existing registration for one physical device, if any — what
/// <see cref="RegisterDeviceCommandHandler"/> upserts against, so re-registering the same device
/// (e.g. on every app launch) never creates a duplicate row.</summary>
public sealed class DeviceRegistrationByUserAndDeviceIdSpecification : ISpecification<DeviceRegistration>
{
    public DeviceRegistrationByUserAndDeviceIdSpecification(TenantId tenantId, UserId userId, string deviceId)
    {
        Criteria = d => d.TenantId == tenantId && d.UserId == userId && d.DeviceId == deviceId;
    }

    public Expression<Func<DeviceRegistration, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<DeviceRegistration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<DeviceRegistration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
