namespace Vespera.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
