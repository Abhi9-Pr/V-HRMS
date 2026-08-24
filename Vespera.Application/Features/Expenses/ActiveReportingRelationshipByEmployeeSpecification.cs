using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Expenses;

public sealed class ActiveReportingRelationshipByEmployeeSpecification : ISpecification<ReportingRelationship>
{
    public ActiveReportingRelationshipByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId, DateOnly asOf)
    {
        Criteria = relationship =>
            relationship.TenantId == tenantId && relationship.EmployeeId == employeeId
            && relationship.ValidFrom <= asOf && (relationship.ValidTo == null || relationship.ValidTo >= asOf);
    }

    public Expression<Func<ReportingRelationship, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<ReportingRelationship, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<ReportingRelationship, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
