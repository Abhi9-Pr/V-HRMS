using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vespera.Application.Abstractions.Identity;

namespace Vespera.Infrastructure.Identity;

/// <summary>
/// Interim <see cref="ICurrentUser"/>, reading whatever <c>HttpContext.User</c> already carries.
/// See <see cref="HttpTenantContext"/> for why this is intentionally minimal — there is no
/// authentication middleware yet, so claims are empty/anonymous until that phase adds it, and
/// this adapter is a safe, swap-in-compatible placeholder in the meantime.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        UserId = IsAuthenticated && Guid.TryParse(user!.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;

        Email = IsAuthenticated ? user!.FindFirstValue(ClaimTypes.Email) : null;

        Roles = IsAuthenticated
            ? user!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
            : Array.Empty<string>();

        IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
    }

    public Guid? UserId { get; }

    public string? Email { get; }

    public bool IsAuthenticated { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public string? IpAddress { get; }
}
