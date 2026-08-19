using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vespera.Infrastructure.Identity;

namespace Vespera.Api.Hubs;

/// <summary>JWT-authenticated. Joins per-user (<c>user:{id}</c>) and per-tenant
/// (<c>tenant:{id}</c>) groups on connect, read straight from the token's own claims — the same
/// claims the rest of the API trusts.</summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = Context.User?.FindFirstValue(JwtClaimTypes.TenantId);

        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        if (tenantId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        }

        await base.OnConnectedAsync();
    }
}
