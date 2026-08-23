using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Domain.Services;

/// <summary>
/// Validates a <see cref="SalaryStructure"/>'s formula graph before it can be saved: no two lines
/// for the same component, at most one <see cref="SalaryComponentFormulaKind.RemainderOfCtc"/>
/// line, every formula reference points at a component actually present in the structure, and —
/// the core check — no cycle among <see cref="PercentageOfComponent"/>/<see cref="SumOfComponents"/>
/// references (e.g. "A = 50% of B" and "B = 50% of A" would never converge). Plain DFS with a
/// visiting/visited pair, since a salary structure has at most a few dozen lines.
/// </summary>
public static class SalaryComponentGraphValidator
{
    public static Result Validate(IReadOnlyList<SalaryStructureLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var seenComponentIds = new HashSet<SalaryComponentId>();
        foreach (var line in lines)
        {
            if (!seenComponentIds.Add(line.ComponentId))
            {
                return Result.Failure(Error.Validation(
                    "salary_structure.duplicate_component", $"Component {line.ComponentId.Value} appears more than once in this structure."));
            }
        }

        if (lines.Count(line => line.Formula.Kind == SalaryComponentFormulaKind.RemainderOfCtc) > 1)
        {
            return Result.Failure(Error.Validation(
                "salary_structure.multiple_remainders", "A salary structure can have at most one remainder-of-CTC line."));
        }

        var byComponent = lines.ToDictionary(line => line.ComponentId, line => line.Formula);
        foreach (var line in lines)
        {
            foreach (var reference in line.Formula.References)
            {
                if (!byComponent.ContainsKey(reference))
                {
                    return Result.Failure(Error.Validation(
                        "salary_structure.unknown_reference",
                        $"Component {line.ComponentId.Value}'s formula references a component that is not part of this structure."));
                }
            }
        }

        var visiting = new HashSet<SalaryComponentId>();
        var visited = new HashSet<SalaryComponentId>();
        foreach (var line in lines)
        {
            if (HasCycle(line.ComponentId, byComponent, visiting, visited))
            {
                return Result.Failure(Error.Validation(
                    "salary_structure.formula_cycle", $"Circular formula reference detected starting at component {line.ComponentId.Value}."));
            }
        }

        return Result.Success();
    }

    private static bool HasCycle(
        SalaryComponentId id, IReadOnlyDictionary<SalaryComponentId, SalaryComponentFormula> byComponent,
        HashSet<SalaryComponentId> visiting, HashSet<SalaryComponentId> visited)
    {
        if (visited.Contains(id))
        {
            return false;
        }

        if (!visiting.Add(id))
        {
            return true;
        }

        foreach (var reference in byComponent[id].References)
        {
            if (HasCycle(reference, byComponent, visiting, visited))
            {
                return true;
            }
        }

        visiting.Remove(id);
        visited.Add(id);
        return false;
    }
}
