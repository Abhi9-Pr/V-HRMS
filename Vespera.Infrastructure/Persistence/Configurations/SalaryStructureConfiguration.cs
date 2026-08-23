using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Infrastructure.Persistence.Configurations;

/// <summary>
/// <see cref="SalaryStructureLine.Formula"/> is mapped as a single delimited string column, not a
/// nested owned type: <see cref="SalaryComponentFormula"/>'s constructor parameter names don't line
/// up with its property names closely enough for EF's constructor binding, the same reason
/// <see cref="Money"/> gets a scalar conversion everywhere else in this codebase rather than
/// <c>OwnsOne</c> (see <see cref="StatutoryRuleSetConfiguration"/>).
/// </summary>
public sealed class SalaryStructureConfiguration : TenantScopedReferenceEntityConfiguration<SalaryStructure, SalaryStructureId>
{
    protected override void ConfigureEntity(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new SalaryStructureId(value))
            .ValueGeneratedNever();

        builder.Property(e => e.EmployeeId).HasConversion(id => id.Value, value => new EmployeeId(value));
        builder.HasIndex(e => new { e.TenantId, e.EmployeeId });

        builder.OwnsMany(e => e.Lines, lines =>
        {
            lines.ToTable("SalaryStructureLines");
            lines.Property(l => l.ComponentId).HasConversion(id => id.Value, value => new SalaryComponentId(value));
            lines.Property(l => l.Formula)
                .HasConversion(formula => SerializeFormula(formula), value => DeserializeFormula(value))
                .HasMaxLength(1024);
        });
    }

    private static string SerializeFormula(SalaryComponentFormula formula) => formula.Kind switch
    {
        SalaryComponentFormulaKind.FixedAmount =>
            $"FixedAmount|{formula.FixedAmountValue!.Amount.ToString(CultureInfo.InvariantCulture)}|{formula.FixedAmountValue.Currency}",
        SalaryComponentFormulaKind.PercentageOfComponent =>
            $"PercentageOfComponent|{formula.ReferenceComponentId!.Value.Value}|{formula.Percent!.Value.ToString(CultureInfo.InvariantCulture)}",
        SalaryComponentFormulaKind.SumOfComponents =>
            $"SumOfComponents|{string.Join(',', formula.SummedComponentIds.Select(id => id.Value))}",
        SalaryComponentFormulaKind.RemainderOfCtc => "RemainderOfCtc",
        _ => throw new InvalidOperationException($"Unhandled formula kind '{formula.Kind}'."),
    };

    private static SalaryComponentFormula DeserializeFormula(string value)
    {
        var parts = value.Split('|');
        return parts[0] switch
        {
            "FixedAmount" => SalaryComponentFormula.FixedAmount(
                Money.Of(decimal.Parse(parts[1], CultureInfo.InvariantCulture), Enum.Parse<Currency>(parts[2]))),
            "PercentageOfComponent" => SalaryComponentFormula.PercentageOfComponent(
                new SalaryComponentId(Guid.Parse(parts[1])), decimal.Parse(parts[2], CultureInfo.InvariantCulture)),
            "SumOfComponents" => SalaryComponentFormula.SumOfComponents(
                [.. parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(id => new SalaryComponentId(Guid.Parse(id)))]),
            "RemainderOfCtc" => SalaryComponentFormula.RemainderOfCtc(),
            _ => throw new InvalidOperationException($"Unrecognized salary component formula payload '{value}'."),
        };
    }
}
