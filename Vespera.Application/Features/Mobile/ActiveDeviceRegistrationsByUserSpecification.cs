using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

/// <summary>Every device a user has push-active — what <c>PushNotificationChannel</c> fans a
/// notification out to. Uses <see cref="IReadRepositoryAdmin{T}"/> because the dispatch path runs
/// with no ambient tenant context (see <c>ApprovalStepAssignedNotificationHandler</c>'s doc
/// comment) — the tenant id travels explicitly via <c>NotificationMessage.Metadata</c> instead.</summary>
public sealed class ActiveDeviceRegistrationsByUserSpecification : ISpecification<DeviceRegistration>
{
    public ActiveDeviceRegistrationsByUserSpecification(TenantId tenantId, UserId userId)
    {
        Criteria = d => d.TenantId == tenantId && d.UserId == userId && d.IsActive;
    }

    public Expression<Func<DeviceRegistration, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<DeviceRegistration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<DeviceRegistration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
