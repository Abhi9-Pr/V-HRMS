using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public sealed class SalaryStructureLine : ValueObject
{
    private SalaryStructureLine(SalaryComponentId componentId, Money amount)
    {
        ComponentId = componentId;
        Amount = amount;
    }

    public SalaryComponentId ComponentId { get; }

    public Money Amount { get; }

    public static SalaryStructureLine Of(SalaryComponentId componentId, Money amount) => new(componentId, amount);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ComponentId;
        yield return Amount;
    }
}

public readonly record struct SalaryStructureId(Guid Value)
{
    public static SalaryStructureId New() => new(Guid.NewGuid());
}

public sealed class SalaryStructure : EffectiveDated<SalaryStructureId>, ITenantScoped
{
    private readonly List<SalaryStructureLine> _lines;

    private SalaryStructure(
        SalaryStructureId id, TenantId tenantId, EmployeeId employeeId, IReadOnlyList<SalaryStructureLine> lines,
        DateOnly validFrom, DateOnly? validTo)
        : base(id, validFrom, validTo)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        _lines = [.. lines];
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public IReadOnlyList<SalaryStructureLine> Lines => _lines.AsReadOnly();

    public static Result<SalaryStructure> Create(
        TenantId tenantId, EmployeeId employeeId, IReadOnlyList<SalaryStructureLine> lines, DateOnly validFrom, DateOnly? validTo)
    {
        if (lines is null || lines.Count == 0)
        {
            return Result.Failure<SalaryStructure>(Error.Validation("salary_structure.no_lines", "A salary structure needs at least one line."));
        }

        return Result.Success(new SalaryStructure(SalaryStructureId.New(), tenantId, employeeId, lines, validFrom, validTo));
    }

    /// <summary>Sum of all lines. Currency-mismatched lines throw via Money's own guard.</summary>
    public Money GrossMonthly() => _lines.Skip(1).Aggregate(_lines[0].Amount, (total, line) => total + line.Amount);

    public Result EndOn(DateOnly validTo) => Close(validTo);
}
