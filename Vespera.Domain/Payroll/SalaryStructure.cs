using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Services;

namespace Vespera.Domain.Payroll;

public sealed class SalaryStructureLine : ValueObject
{
    private SalaryStructureLine(SalaryComponentId componentId, SalaryComponentFormula formula)
    {
        ComponentId = componentId;
        Formula = formula;
    }

    public SalaryComponentId ComponentId { get; }

    public SalaryComponentFormula Formula { get; }

    public static SalaryStructureLine Of(SalaryComponentId componentId, SalaryComponentFormula formula) => new(componentId, formula);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ComponentId;
        yield return Formula;
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

        var graphResult = SalaryComponentGraphValidator.Validate(lines);
        if (graphResult.IsFailure)
        {
            return Result.Failure<SalaryStructure>(graphResult.Error);
        }

        return Result.Success(new SalaryStructure(SalaryStructureId.New(), tenantId, employeeId, lines, validFrom, validTo));
    }

    public Result EndOn(DateOnly validTo) => Close(validTo);
}
