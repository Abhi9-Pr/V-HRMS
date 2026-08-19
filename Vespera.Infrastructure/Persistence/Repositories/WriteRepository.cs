using Vespera.Application.Abstractions.Persistence;

namespace Vespera.Infrastructure.Persistence.Repositories;

/// <summary>One generic implementation for every entity, mirroring <see cref="ReadRepository{T}"/>.</summary>
public sealed class WriteRepository<T> : IWriteRepository<T>
    where T : class
{
    private readonly VesperaDbContext _dbContext;

    public WriteRepository(VesperaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken) =>
        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => _dbContext.Set<T>().Update(entity);

    public void Remove(T entity) => _dbContext.Set<T>().Remove(entity);
}
