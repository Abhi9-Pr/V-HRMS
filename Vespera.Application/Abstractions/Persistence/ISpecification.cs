using System.Linq.Expressions;

namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// Describes a query against <typeparamref name="T"/> without leaking any persistence
/// technology into the Application layer. Implementations that answer CountAsync must apply
/// <see cref="Criteria"/> but ignore <see cref="Paging"/>.
/// </summary>
public interface ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
