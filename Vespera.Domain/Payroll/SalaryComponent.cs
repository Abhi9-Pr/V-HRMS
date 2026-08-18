using Vespera.Domain.Common;

namespace Vespera.Domain.Payroll;

public readonly record struct SalaryComponentId(Guid Value)
{
    public static SalaryComponentId New() => new(Guid.NewGuid());
}

public enum SalaryComponentType
{
    Earning,
    Deduction,
    Reimbursement,
    StatutoryContribution,
}

public sealed class SalaryComponent : AuditableTenantAggregateRoot<SalaryComponentId>
{
    private SalaryComponent(
        SalaryComponentId id, TenantId tenantId, string name, SalaryComponentType componentType, bool isTaxable,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        ComponentType = componentType;
        IsTaxable = isTaxable;
    }

    public string Name { get; private set; }

    public SalaryComponentType ComponentType { get; }

    public bool IsTaxable { get; private set; }

    public static Result<SalaryComponent> Create(
        TenantId tenantId, string name, SalaryComponentType componentType, bool isTaxable,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<SalaryComponent>(Error.Validation("salary_component.name_required", "Salary component name is required."));
        }

        return Result.Success(new SalaryComponent(SalaryComponentId.New(), tenantId, name.Trim(), componentType, isTaxable, occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("salary_component.name_required", "Salary component name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result UpdateTaxability(bool isTaxable, DateTimeOffset occurredOn, string modifiedBy)
    {
        IsTaxable = isTaxable;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
