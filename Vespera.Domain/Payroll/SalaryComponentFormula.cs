using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public enum SalaryComponentFormulaKind
{
    FixedAmount,
    PercentageOfComponent,
    SumOfComponents,
    RemainderOfCtc,
}

/// <summary>
/// How one <see cref="SalaryStructureLine"/> derives its monthly amount — a small closed algebra
/// (not a general expression parser) that covers every case the brief calls for: a flat number
/// ("Special Allowance = ₹5,000"), a percentage of another component ("HRA = 40% of Basic"), a sum
/// of other components, or "whatever's left of CTC after every other line" (at most one line per
/// structure may use <see cref="RemainderOfCtc"/> — enforced by <see cref="SalaryStructure.Create"/>).
/// <see cref="References"/> is what <see cref="Services.SalaryComponentGraphValidator"/> walks for
/// cycle detection and what <see cref="Services.SalaryStructureResolver"/> walks to resolve lines
/// in dependency order.
/// </summary>
public sealed class SalaryComponentFormula : ValueObject
{
    private readonly List<SalaryComponentId> _summedComponentIds;

    private SalaryComponentFormula(
        SalaryComponentFormulaKind kind, Money? fixedAmount, SalaryComponentId? referenceComponentId,
        decimal? percent, IReadOnlyList<SalaryComponentId>? summedComponentIds)
    {
        Kind = kind;
        FixedAmountValue = fixedAmount;
        ReferenceComponentId = referenceComponentId;
        Percent = percent;
        _summedComponentIds = [.. summedComponentIds ?? []];
    }

    public SalaryComponentFormulaKind Kind { get; }

    public Money? FixedAmountValue { get; }

    public SalaryComponentId? ReferenceComponentId { get; }

    public decimal? Percent { get; }

    public IReadOnlyList<SalaryComponentId> SummedComponentIds => _summedComponentIds.AsReadOnly();

    public static SalaryComponentFormula FixedAmount(Money amount) =>
        new(SalaryComponentFormulaKind.FixedAmount, amount, null, null, null);

    public static SalaryComponentFormula PercentageOfComponent(SalaryComponentId referenceComponentId, decimal percent)
    {
        if (percent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "A percentage-of-component formula must be a positive percentage.");
        }

        return new(SalaryComponentFormulaKind.PercentageOfComponent, null, referenceComponentId, percent, null);
    }

    public static SalaryComponentFormula SumOfComponents(IReadOnlyList<SalaryComponentId> componentIds)
    {
        if (componentIds is null || componentIds.Count == 0)
        {
            throw new ArgumentException("A sum-of-components formula needs at least one referenced component.", nameof(componentIds));
        }

        return new(SalaryComponentFormulaKind.SumOfComponents, null, null, null, componentIds);
    }

    public static SalaryComponentFormula RemainderOfCtc() => new(SalaryComponentFormulaKind.RemainderOfCtc, null, null, null, null);

    /// <summary>Every component this formula reads from another line for — the graph edges for
    /// cycle detection and dependency ordering. Empty for <see cref="FixedAmount"/> and
    /// <see cref="RemainderOfCtc"/>: the latter depends on "every other line," which the resolver
    /// handles as a special last-resolved case rather than an explicit graph edge.</summary>
    public IReadOnlyList<SalaryComponentId> References => Kind switch
    {
        SalaryComponentFormulaKind.PercentageOfComponent => [ReferenceComponentId!.Value],
        SalaryComponentFormulaKind.SumOfComponents => SummedComponentIds,
        _ => [],
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Kind;
        yield return FixedAmountValue;
        yield return ReferenceComponentId;
        yield return Percent;
        foreach (var id in _summedComponentIds)
        {
            yield return id;
        }
    }
}
