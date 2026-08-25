using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed class QuarantinedBiometricPunchByIdSpecification : ISpecification<QuarantinedBiometricPunch>
{
    public QuarantinedBiometricPunchByIdSpecification(TenantId tenantId, QuarantinedBiometricPunchId id)
    {
        Criteria = entry => entry.TenantId == tenantId && entry.Id == id;
    }

    public Expression<Func<QuarantinedBiometricPunch, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<QuarantinedBiometricPunch, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<QuarantinedBiometricPunch, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
