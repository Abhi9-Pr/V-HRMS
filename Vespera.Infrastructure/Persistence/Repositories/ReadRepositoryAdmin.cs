using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Persistence;

namespace Vespera.Infrastructure.Persistence.Repositories;

/// <summary>
/// The one and only place <c>IgnoreQueryFilters</c> is called in the codebase — the documented
/// escape hatch around the tenant/soft-delete global query filters. See
/// <see cref="IReadRepositoryAdmin{T}"/> for why this is a separate port rather than a method on
/// <see cref="IReadRepository{T}"/>.
/// </summary>
public sealed class ReadRepositoryAdmin<T> : IReadRepositoryAdmin<T>
    where T : class
{
    private readonly VesperaDbContext _dbContext;

    public ReadRepositoryAdmin(VesperaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<T>> ListIgnoringFiltersAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        await SpecificationEvaluator.Apply(_dbContext.Set<T>().IgnoreQueryFilters(), specification).ToListAsync(cancellationToken);
}
