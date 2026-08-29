using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;

namespace Vespera.Application.Common;

/// <summary>
/// Mobile delta-sync plumbing (see docs/api-mobile-contract.md): given a per-feature
/// specification expressing "changed since <see cref="DeltaSyncRequest.Since"/>", returns
/// upserts and tombstones in the shared <see cref="DeltaSyncResult{T}"/> shape. Built entirely on
/// existing ports (<see cref="IReadRepository{T}"/>, <see cref="ISpecification{T}"/>) — no new
/// persistence mechanism. A concrete query handler supplies the entity-specific bits
/// (specification, id, mapping); this class is not wired to any endpoint yet, per the brief.
/// </summary>
public abstract class DeltaSyncQueryHandlerBase<TRequest, TEntity, TDto> : IRequestHandler<TRequest, Result<DeltaSyncResult<TDto>>>
    where TRequest : IRequest<Result<DeltaSyncResult<TDto>>>
    where TEntity : class
{
    private readonly IReadRepository<TEntity> _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    protected DeltaSyncQueryHandlerBase(IReadRepository<TEntity> repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>Extracts the paging/since cursor from the concrete request DTO.</summary>
    protected abstract DeltaSyncRequest GetDeltaSyncRequest(TRequest request);

    /// <summary>Builds the "tenant-scoped (plus whatever else identifies the caller's own record
    /// set), IgnoreQueryFilters-for-soft-deleted" specification — soft-deleted rows must still be
    /// returned (as tombstones), so a concrete implementation typically needs
    /// <c>IReadRepositoryAdmin{TEntity}</c> rather than this base's injected
    /// <c>IReadRepository{TEntity}</c> if tombstones are required; see the interface docs.
    /// Deliberately does NOT filter on <see cref="DeltaSyncRequest.Since"/>, and does not set the
    /// specification's own <c>Paging</c>/<c>OrderBy</c> — see <see cref="Handle"/>'s comment on why
    /// the since-cutoff, ordering, and paging all happen in memory here, not in SQL. Async so a
    /// concrete handler can resolve request-scoped context (e.g. "which entity id does the calling
    /// user's own record correspond to") via a repository call before building the specification's
    /// criteria.</summary>
    protected abstract Task<ISpecification<TEntity>> BuildSpecification(TRequest request, DeltaSyncRequest deltaSync, CancellationToken cancellationToken);

    protected abstract Guid GetId(TEntity entity);

    protected abstract DateTimeOffset GetLastChanged(TEntity entity);

    protected abstract TDto MapToDto(TEntity entity);

    /// <summary>Whether this changed entity should be reported as a tombstone (removed from the
    /// client's local cache) rather than an upsert. Entities with no delete concept at all (e.g.
    /// <c>LeaveRequest</c>, which is only ever status-transitioned, never soft-deleted) always
    /// return <see langword="false"/> here.</summary>
    protected abstract bool IsTombstoned(TEntity entity);

    public async Task<Result<DeltaSyncResult<TDto>>> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var deltaSync = GetDeltaSyncRequest(request);
        var specification = await BuildSpecification(request, deltaSync, cancellationToken);

        var entities = await _repository.ListAsync(specification, cancellationToken);

        // Filtered by the since-cutoff, ordered, and paged here, in memory, rather than via the
        // specification's own Criteria/OrderBy/Paging — every concrete handler already supplies
        // GetLastChanged, so comparing/sorting on it in LINQ-to-objects is free of any provider-
        // specific SQL-translation risk (this codebase's test/dev-fallback provider can't translate
        // a WHERE or ORDER BY expression over a DateTimeOffset column at all). The entity set a
        // delta-sync query matches (one caller's own records) is small enough that fetching the
        // whole set unfiltered-by-date and slicing in memory is the safer choice.
        var changedSince = entities.Where(e => GetLastChanged(e) > deltaSync.Since);
        var ordered = changedSince.OrderBy(GetLastChanged).ToList();
        var page = ordered.Take(deltaSync.PageSize).ToList();

        var upserts = page.Where(e => !IsTombstoned(e)).Select(MapToDto).ToList();
        var tombstonedIds = page.Where(IsTombstoned).Select(GetId).ToList();

        var hasMore = ordered.Count > page.Count;
        var nextCursor = hasMore ? GetLastChanged(page[^1]).ToString("O") : null;

        return Result.Success(new DeltaSyncResult<TDto>(upserts, tombstonedIds, _dateTimeProvider.UtcNow, nextCursor));
    }
}
