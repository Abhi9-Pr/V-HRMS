namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// The documented escape hatch around the tenant/soft-delete global query filters. Deliberately a
/// separate interface from <see cref="IReadRepository{T}"/> (ISP) so an ordinary handler never
/// sees a filter-bypassing method on the port it injects — only code that explicitly asks for
/// <see cref="IReadRepositoryAdmin{T}"/> can read across tenants or see soft-deleted rows, and
/// that short list of callers is exactly what an architecture test can enforce. Reserved for
/// admin/support tooling and system-wide background jobs (e.g. the DPDP retention sweep), never
/// for a normal feature query.
/// </summary>
public interface IReadRepositoryAdmin<T>
    where T : class
{
    public Task<IReadOnlyList<T>> ListIgnoringFiltersAsync(ISpecification<T> specification, CancellationToken cancellationToken);
}
