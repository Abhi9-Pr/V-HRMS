using Vespera.Domain.Common;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Services;

/// <summary>
/// Resolves a <see cref="SalaryStructure"/>'s formula lines to concrete monthly <see cref="Money"/>
/// amounts against a target CTC — "CTC-down". Lines are resolved in dependency order (a
/// topological sort over <see cref="SalaryComponentFormula.References"/>, which
/// <see cref="SalaryComponentGraphValidator"/> already guarantees is acyclic) so "HRA = 40% of
/// Basic" always sees a resolved Basic. The single <see cref="SalaryComponentFormulaKind.RemainderOfCtc"/>
/// line, if present, is always resolved last regardless of its position in the structure, since by
/// definition it needs every other line's amount first. Gross-up (target net → required CTC) is
/// deliberately not here — it's an iterative search over full pipeline runs, which belongs in the
/// Application-layer orchestrator that can actually run the pipeline, not in this pure resolver.
/// </summary>
public static class SalaryStructureResolver
{
    public static Result<IReadOnlyDictionary<SalaryComponentId, Money>> ResolveMonthly(SalaryStructure structure, Money targetCtc)
    {
        ArgumentNullException.ThrowIfNull(structure);
        ArgumentNullException.ThrowIfNull(targetCtc);

        var graphResult = SalaryComponentGraphValidator.Validate(structure.Lines);
        if (graphResult.IsFailure)
        {
            return Result.Failure<IReadOnlyDictionary<SalaryComponentId, Money>>(graphResult.Error);
        }

        var byComponent = structure.Lines.ToDictionary(line => line.ComponentId, line => line.Formula);
        var remainderLine = structure.Lines.FirstOrDefault(line => line.Formula.Kind == SalaryComponentFormulaKind.RemainderOfCtc);

        var resolved = new Dictionary<SalaryComponentId, Money>();
        var resolving = new HashSet<SalaryComponentId>();

        foreach (var line in structure.Lines)
        {
            if (line.ComponentId == remainderLine?.ComponentId)
            {
                continue;
            }

            Resolve(line.ComponentId, byComponent, resolved, resolving, targetCtc.Currency);
        }

        if (remainderLine is not null)
        {
            var sumOfOthers = resolved.Values.Aggregate(Money.Zero(targetCtc.Currency), (total, amount) => total + amount);
            var remainder = targetCtc - sumOfOthers;
            if (remainder.Amount < 0)
            {
                return Result.Failure<IReadOnlyDictionary<SalaryComponentId, Money>>(Error.Validation(
                    "salary_structure.ctc_exceeded",
                    "The structure's fixed and percentage-based lines already exceed the target CTC — nothing is left for the remainder line."));
            }

            resolved[remainderLine.ComponentId] = remainder;
        }

        return Result.Success<IReadOnlyDictionary<SalaryComponentId, Money>>(resolved);
    }

    private static Money Resolve(
        SalaryComponentId componentId, IReadOnlyDictionary<SalaryComponentId, SalaryComponentFormula> byComponent,
        Dictionary<SalaryComponentId, Money> resolved, HashSet<SalaryComponentId> resolving, Currency currency)
    {
        if (resolved.TryGetValue(componentId, out var already))
        {
            return already;
        }

        resolving.Add(componentId);
        var formula = byComponent[componentId];
        var amount = formula.Kind switch
        {
            SalaryComponentFormulaKind.FixedAmount => formula.FixedAmountValue!,
            SalaryComponentFormulaKind.PercentageOfComponent => Money.Of(
                Math.Round(Resolve(formula.ReferenceComponentId!.Value, byComponent, resolved, resolving, currency).Amount * formula.Percent!.Value / 100m, 2),
                currency),
            SalaryComponentFormulaKind.SumOfComponents => formula.SummedComponentIds
                .Select(id => Resolve(id, byComponent, resolved, resolving, currency))
                .Aggregate(Money.Zero(currency), (total, amount) => total + amount),
            SalaryComponentFormulaKind.RemainderOfCtc => Money.Zero(currency),
            _ => throw new InvalidOperationException($"Unhandled formula kind '{formula.Kind}'."),
        };

        resolving.Remove(componentId);
        resolved[componentId] = amount;
        return amount;
    }
}
