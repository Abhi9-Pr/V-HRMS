using Microsoft.EntityFrameworkCore;
using Vespera.Application.Abstractions.Persistence;

namespace Vespera.Infrastructure.Persistence.Repositories;

/// <summary>
/// Translates an <see cref="ISpecification{T}"/> into an <see cref="IQueryable{T}"/>. One
/// evaluator for every entity, not one per entity — new specifications just implement the
/// interface, this class never changes (OCP).
/// </summary>
internal static class SpecificationEvaluator
{
    public static IQueryable<T> Apply<T>(IQueryable<T> source, ISpecification<T> specification)
        where T : class
    {
        var query = source;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy.Count > 0)
        {
            var (firstKey, firstDescending) = specification.OrderBy[0];
            var ordered = firstDescending ? query.OrderByDescending(firstKey) : query.OrderBy(firstKey);

            for (var i = 1; i < specification.OrderBy.Count; i++)
            {
                var (key, descending) = specification.OrderBy[i];
                ordered = descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
            }

            query = ordered;
        }

        if (specification.Paging is { } paging)
        {
            query = query.Skip(paging.Skip).Take(paging.Take);
        }

        return query;
    }

    /// <summary>Ignores <see cref="ISpecification{T}.Paging"/> — see the interface's own doc comment.</summary>
    public static IQueryable<T> ApplyIgnoringPaging<T>(IQueryable<T> source, ISpecification<T> specification)
        where T : class
    {
        var query = source;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        return specification.Includes.Aggregate(query, (current, include) => current.Include(include));
    }
}
