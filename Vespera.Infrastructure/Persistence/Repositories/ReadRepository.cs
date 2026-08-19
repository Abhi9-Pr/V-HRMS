using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Persistence;

namespace Vespera.Infrastructure.Persistence.Repositories;

/// <summary>One generic implementation for every entity — see CONTRIBUTING-slices.md's
/// non-negotiable against one repository class per entity.</summary>
public sealed class ReadRepository<T> : IReadRepository<T>
    where T : class
{
    private readonly VesperaDbContext _dbContext;

    public ReadRepository(VesperaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        SpecificationEvaluator.Apply(_dbContext.Set<T>(), specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        await SpecificationEvaluator.Apply(_dbContext.Set<T>(), specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        SpecificationEvaluator.ApplyIgnoringPaging(_dbContext.Set<T>(), specification).CountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken) =>
        SpecificationEvaluator.ApplyIgnoringPaging(_dbContext.Set<T>(), specification).AnyAsync(cancellationToken);
}
