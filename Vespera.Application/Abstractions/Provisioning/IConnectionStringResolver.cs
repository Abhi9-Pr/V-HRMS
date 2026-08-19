namespace Vespera.Application.Abstractions.Provisioning;

public interface IConnectionStringResolver
{
    public Task<string> ResolveAsync(CancellationToken cancellationToken);
}
