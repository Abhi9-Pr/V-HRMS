namespace Vespera.Application.Abstractions.Persistence;

public interface IReadRepository<T>
    where T : class
{
    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken);

    public Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken);
}
