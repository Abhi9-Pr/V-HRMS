namespace Vespera.Application.Abstractions.Identity;

public interface ICurrentUser
{
    public Guid? UserId { get; }

    public string? Email { get; }

    public bool IsAuthenticated { get; }

    public IReadOnlyCollection<string> Roles { get; }
}
