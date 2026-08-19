namespace Vespera.Application.Abstractions.Identity;

public interface ICurrentUser
{
    public Guid? UserId { get; }

    public string? Email { get; }

    public bool IsAuthenticated { get; }

    public IReadOnlyCollection<string> Roles { get; }

    /// <summary>Caller's IP address, when known (e.g. from the current HTTP request). Null for
    /// background/system-initiated work. Consumed by audit logging, never for authorization.</summary>
    public string? IpAddress { get; }
}
