using Vespera.Domain.Common;

namespace Vespera.Domain.Eis;

public readonly record struct DepartmentId(Guid Value)
{
    public static DepartmentId New() => new(Guid.NewGuid());
}

public sealed class Department : AuditableTenantAggregateRoot<DepartmentId>
{
    private Department(
        DepartmentId id, TenantId tenantId, string name, string code, DepartmentId? parentDepartmentId,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        Code = code;
        ParentDepartmentId = parentDepartmentId;
    }

    public string Name { get; private set; }

    public string Code { get; }

    public DepartmentId? ParentDepartmentId { get; private set; }

    public EmployeeId? HeadEmployeeId { get; private set; }

    public static Result<Department> Create(
        TenantId tenantId, string name, string code, DepartmentId? parentDepartmentId,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Department>(Error.Validation("department.name_required", "Department name is required."));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Department>(Error.Validation("department.code_required", "Department code is required."));
        }

        return Result.Success(new Department(DepartmentId.New(), tenantId, name.Trim(), code.Trim(), parentDepartmentId, occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("department.name_required", "Department name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reparent(DepartmentId? parentDepartmentId, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (parentDepartmentId == Id)
        {
            return Result.Failure(Error.Validation("department.self_parent", "A department cannot be its own parent."));
        }

        ParentDepartmentId = parentDepartmentId;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result AssignHead(EmployeeId headEmployeeId, DateTimeOffset occurredOn, string modifiedBy)
    {
        HeadEmployeeId = headEmployeeId;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
