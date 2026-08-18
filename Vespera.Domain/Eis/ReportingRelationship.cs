using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct ReportingRelationshipId(Guid Value)
{
    public static ReportingRelationshipId New() => new(Guid.NewGuid());
}

public sealed class ReportingRelationship : EffectiveDated<ReportingRelationshipId>, ITenantScoped
{
    private ReportingRelationship(
        ReportingRelationshipId id, TenantId tenantId, EmployeeId employeeId, EmployeeId managerId,
        DateOnly validFrom, DateOnly? validTo)
        : base(id, validFrom, validTo)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        ManagerId = managerId;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public EmployeeId ManagerId { get; }

    public static Result<ReportingRelationship> Create(
        TenantId tenantId, EmployeeId employeeId, EmployeeId managerId, DateOnly validFrom, DateOnly? validTo)
    {
        if (employeeId == managerId)
        {
            return Result.Failure<ReportingRelationship>(
                Error.Validation("reporting_relationship.self_reporting", "An employee cannot report to themselves."));
        }

        return Result.Success(new ReportingRelationship(
            ReportingRelationshipId.New(), tenantId, employeeId, managerId, validFrom, validTo));
    }

    public Result EndOn(DateOnly validTo) => Close(validTo);
}
