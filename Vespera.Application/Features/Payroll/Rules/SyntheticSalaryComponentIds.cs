using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll.Rules;

/// <summary>
/// Fixed, well-known <see cref="SalaryComponentId"/>s for pipeline-computed lines that aren't part
/// of any employee's configured <c>SalaryStructure</c> — Loss of Pay, the statutory deductions,
/// income tax. They don't correspond to real <c>SalaryComponent</c> rows; they exist purely so
/// every run of the pipeline emits the exact same id for "Provident Fund" every time (required for
/// two dry-runs on the same frozen inputs to be byte-identical — a freshly-generated Guid per run
/// would fail that on its own).
/// </summary>
/// <remarks>Public rather than <c>internal</c> — this codebase has no <c>InternalsVisibleTo</c>
/// wiring anywhere, so an <c>internal</c> type here would be unreachable from its own test
/// project.</remarks>
public static class SyntheticSalaryComponentIds
{
    public static readonly SalaryComponentId LossOfPay = new(Guid.Parse("00000000-0000-0000-0000-000000000101"));
    public static readonly SalaryComponentId ProvidentFund = new(Guid.Parse("00000000-0000-0000-0000-000000000201"));
    public static readonly SalaryComponentId EmployeeStateInsurance = new(Guid.Parse("00000000-0000-0000-0000-000000000202"));
    public static readonly SalaryComponentId ProfessionalTax = new(Guid.Parse("00000000-0000-0000-0000-000000000203"));
    public static readonly SalaryComponentId IncomeTax = new(Guid.Parse("00000000-0000-0000-0000-000000000301"));
}
