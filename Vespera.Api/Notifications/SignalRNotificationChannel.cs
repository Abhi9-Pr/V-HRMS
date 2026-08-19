using Microsoft.AspNetCore.SignalR;
using Vespera.Api.Hubs;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Api.Notifications;

/// <summary>
/// Pushes through <see cref="NotificationHub"/> to the recipient's <c>user:{id}</c> group. Lives
/// in Api (not Infrastructure) because SignalR hubs are an Api-layer concern per AGENTS.md, and
/// this channel needs <see cref="IHubContext{THub}"/> for that specific hub — registering it as
/// one more <see cref="INotificationChannel"/> is the only change; Phase 3's
/// <c>NotificationDispatcher</c> (Infrastructure) already fans out to every registered channel.
/// </summary>
public sealed class SignalRNotificationChannel : INotificationChannel
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationChannel(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public NotificationChannelType Type => NotificationChannelType.InApp;

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken) =>
        await _hubContext.Clients.Group($"user:{message.RecipientId}").SendAsync("notification", message, cancellationToken);
}
