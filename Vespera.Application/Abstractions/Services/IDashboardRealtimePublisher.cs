namespace Vespera.Application.Abstractions.Services;

public sealed record PunchStateChangedPayload(Guid EmployeeId, string Status, string? LastPunchType, DateTimeOffset? LastPunchAt);

public sealed record ApprovalsCountChangedPayload(int PendingCount);

public sealed record AnnouncementPublishedPayload(Guid AnnouncementId, string Title, string Priority, bool IsPinned, DateTimeOffset PublishAt);

/// <summary>Structured, dashboard-shaped real-time push — distinct from
/// <see cref="INotificationDispatcher"/>'s generic title/body toast. Implemented in Api (not
/// Infrastructure) because it needs <c>IHubContext&lt;NotificationHub&gt;</c>, the same reasoning
/// as <c>SignalRNotificationChannel</c>. Every method targets a single recipient's
/// <c>user:{id}</c> SignalR group, resolved by the caller the same way
/// <c>ApprovalStepAssignedNotificationHandler</c> already resolves a recipient <c>User</c> from an
/// <c>EmployeeId</c>.</summary>
public interface IDashboardRealtimePublisher
{
    public Task PublishPunchStateChangedAsync(Guid recipientUserId, PunchStateChangedPayload payload, CancellationToken cancellationToken);

    public Task PublishApprovalsCountChangedAsync(Guid recipientUserId, ApprovalsCountChangedPayload payload, CancellationToken cancellationToken);

    public Task PublishAnnouncementAsync(Guid tenantId, AnnouncementPublishedPayload payload, CancellationToken cancellationToken);
}
