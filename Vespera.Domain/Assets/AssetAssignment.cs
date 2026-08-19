using Vespera.Domain.Assets.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Assets;

public readonly record struct AssetAssignmentId(Guid Value)
{
    public static AssetAssignmentId New() => new(Guid.NewGuid());
}

public sealed class AssetAssignment : AggregateRoot<AssetAssignmentId>, ITenantScoped
{
    private AssetAssignment(AssetAssignmentId id, TenantId tenantId, AssetId assetId, EmployeeId employeeId, DateTimeOffset assignedAt)
        : base(id)
    {
        TenantId = tenantId;
        AssetId = assetId;
        EmployeeId = employeeId;
        AssignedAt = assignedAt;
    }

    public TenantId TenantId { get; }

    public AssetId AssetId { get; }

    public EmployeeId EmployeeId { get; }

    public DateTimeOffset AssignedAt { get; }

    public DateTimeOffset? ReturnedAt { get; private set; }

    public string? ReturnCondition { get; private set; }

    public static AssetAssignment Assign(TenantId tenantId, AssetId assetId, EmployeeId employeeId, DateTimeOffset occurredOn)
    {
        var assignment = new AssetAssignment(AssetAssignmentId.New(), tenantId, assetId, employeeId, occurredOn);
        assignment.Raise(new AssetAssigned(assignment.Id, assetId, employeeId, occurredOn));
        return assignment;
    }

    public Result Return(string condition, DateTimeOffset occurredOn)
    {
        if (ReturnedAt is not null)
        {
            return Result.Failure(Error.Conflict("asset_assignment.already_returned", "This assignment has already been returned."));
        }

        ReturnedAt = occurredOn;
        ReturnCondition = condition;
        return Result.Success();
    }
}
