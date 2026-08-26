using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class InvestmentDeclarationByIdSpecification : ISpecification<InvestmentDeclaration>
{
    public InvestmentDeclarationByIdSpecification(InvestmentDeclarationId id)
    {
        Criteria = declaration => declaration.Id == id;
    }

    public Expression<Func<InvestmentDeclaration, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclaration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclaration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
