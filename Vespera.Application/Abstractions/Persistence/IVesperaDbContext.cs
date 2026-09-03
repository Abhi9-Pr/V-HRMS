namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// Escape hatch for read-side queries that need to project straight off a queryable source
/// (e.g. Mapster's ProjectToType) instead of materializing entities through a repository.
/// Deliberately EF-free: it exposes plain IQueryable, not DbSet — which is also why query
/// execution goes through <see cref="ToListAsync{TEntity}"/>/<see cref="CountAsync{TEntity}"/>
/// here instead of EF Core's own IQueryable extension methods (<c>.ToListAsync()</c>,
/// <c>.CountAsync()</c>): those live in the <c>Microsoft.EntityFrameworkCore</c> package, which
/// Vespera.Application must never reference (see AGENTS.md's dependency rule). The
/// implementation (<c>VesperaDbContext</c>, in Infrastructure) is free to call the real EF Core
/// async methods internally.
/// </summary>
public interface IVesperaDbContext
{
    public IQueryable<TEntity> Set<TEntity>() where TEntity : class;

    public Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken);

    public Task<int> CountAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken);
}
