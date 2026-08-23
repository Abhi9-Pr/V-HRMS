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

    // No `lines` constructor parameter: EF Core cannot constructor-bind an owned-collection
    // navigation. Create() below populates _lines after construction instead.
    private SalaryStructure(SalaryStructureId id, TenantId tenantId, EmployeeId employeeId, DateOnly validFrom, DateOnly? validTo)
        : base(id, validFrom, validTo)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        _lines = [];
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

        var structure = new SalaryStructure(SalaryStructureId.New(), tenantId, employeeId, validFrom, validTo);
        structure._lines.AddRange(lines);
        return Result.Success(structure);
    }

    public Result EndOn(DateOnly validTo) => Close(validTo);
}
