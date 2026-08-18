using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct EmploymentHistoryId(Guid Value)
{
    public static EmploymentHistoryId New() => new(Guid.NewGuid());
}

public enum EmploymentChangeReason
{
    Hire,
    Promotion,
    Transfer,
    Demotion,
    Redesignation,
}

/// <summary>An immutable fact recording what an employee's assignment was from a given date.</summary>
public sealed class EmploymentHistory : Entity<EmploymentHistoryId>
{
    internal EmploymentHistory(
        EmploymentHistoryId id, DepartmentId departmentId, DesignationId designationId, LocationId locationId,
        DateOnly effectiveFrom, EmploymentChangeReason changeReason)
        : base(id)
    {
        DepartmentId = departmentId;
        DesignationId = designationId;
        LocationId = locationId;
        EffectiveFrom = effectiveFrom;
        ChangeReason = changeReason;
    }

    public DepartmentId DepartmentId { get; }

    public DesignationId DesignationId { get; }

    public LocationId LocationId { get; }

    public DateOnly EffectiveFrom { get; }

    public EmploymentChangeReason ChangeReason { get; }
}
