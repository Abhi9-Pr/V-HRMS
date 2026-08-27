using Microsoft.AspNetCore.SignalR;
using Vespera.Api.Hubs;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Api.Notifications;

/// <summary>Structured dashboard push over <see cref="NotificationHub"/>'s existing <c>user:{id}</c>
/// and <c>tenant:{id}</c> groups — distinct from <see cref="SignalRNotificationChannel"/>'s generic
/// title/body toast, this sends the widget-shaped payload itself so the client can update in place
/// without a re-fetch. Lives in Api for the same reason SignalRNotificationChannel does: it needs
/// <see cref="IHubContext{THub}"/> for this specific hub, an Api-layer concern per AGENTS.md.</summary>
public sealed class DashboardRealtimeBroadcaster : IDashboardRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public DashboardRealtimeBroadcaster(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishPunchStateChangedAsync(Guid recipientUserId, PunchStateChangedPayload payload, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group($"user:{recipientUserId}").SendAsync("dashboard:punchStateChanged", payload, cancellationToken);

    public Task PublishApprovalsCountChangedAsync(Guid recipientUserId, ApprovalsCountChangedPayload payload, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group($"user:{recipientUserId}").SendAsync("dashboard:approvalsCountChanged", payload, cancellationToken);

    public Task PublishAnnouncementAsync(Guid tenantId, AnnouncementPublishedPayload payload, CancellationToken cancellationToken) =>
        _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync("dashboard:announcementPublished", payload, cancellationToken);
}
