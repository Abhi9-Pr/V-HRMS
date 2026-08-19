namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// Escape hatch for read-side queries that need to project straight off a queryable source
/// (e.g. Mapster's ProjectToType) instead of materializing entities through a repository.
/// Deliberately EF-free: it exposes plain IQueryable, not DbSet.
/// </summary>
public interface IVesperaDbContext
{
    public IQueryable<TEntity> Set<TEntity>() where TEntity : class;
}
