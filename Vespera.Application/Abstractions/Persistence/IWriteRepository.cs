namespace Vespera.Application.Abstractions.Persistence;

public interface IWriteRepository<T>
    where T : class
{
    public Task AddAsync(T entity, CancellationToken cancellationToken);

    public void Update(T entity);

    public void Remove(T entity);
}
