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
    where TEntity : class, ISoftDeletable
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

    /// <summary>Builds the "changed since, tenant-scoped, page-sized, IgnoreQueryFilters-for-soft-deleted"
    /// specification — soft-deleted rows must still be returned (as tombstones), so a concrete
    /// implementation typically needs <c>IReadRepositoryAdmin{TEntity}</c> rather than this base's
    /// injected <c>IReadRepository{TEntity}</c> if tombstones are required; see the interface docs.</summary>
    protected abstract ISpecification<TEntity> BuildSpecification(TRequest request, DeltaSyncRequest deltaSync);

    protected abstract Guid GetId(TEntity entity);

    protected abstract DateTimeOffset GetLastChanged(TEntity entity);

    protected abstract TDto MapToDto(TEntity entity);

    public async Task<Result<DeltaSyncResult<TDto>>> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var deltaSync = GetDeltaSyncRequest(request);
        var specification = BuildSpecification(request, deltaSync);

        var entities = await _repository.ListAsync(specification, cancellationToken);

        var upserts = entities.Where(e => !e.IsDeleted).Select(MapToDto).ToList();
        var tombstonedIds = entities.Where(e => e.IsDeleted).Select(GetId).ToList();

        var hasMore = entities.Count >= deltaSync.PageSize;
        var nextCursor = hasMore ? GetLastChanged(entities[^1]).ToString("O") : null;

        return Result.Success(new DeltaSyncResult<TDto>(upserts, tombstonedIds, _dateTimeProvider.UtcNow, nextCursor));
    }
}
