using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PayrollSettingsByTenantSpecification : ISpecification<PayrollSettings>
{
    public PayrollSettingsByTenantSpecification(TenantId tenantId)
    {
        Criteria = settings => settings.TenantId == tenantId;
    }

    public Expression<Func<PayrollSettings, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PayrollSettings, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PayrollSettings, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
