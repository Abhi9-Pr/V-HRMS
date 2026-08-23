using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

/// <summary>A resolved ad-hoc line — a voluntary deduction election or an approved reimbursement
/// claim — handed in already-decided; <see cref="VoluntaryDeductionsRule"/>/<see cref="ReimbursementsRule"/>
/// (in Vespera.Application) just turn these into <see cref="PayrollComponentLine"/>s. Deliberately
/// not a full claims-workflow aggregate — that's a larger scope than this phase covers.</summary>
public sealed record PayrollAdHocLine(SalaryComponentId ComponentId, string ComponentName, Money Amount);

/// <summary>
/// The per-employee working context threaded through the rules pipeline: everything a stage might
/// need to read (resolved salary structure, statutory rates, tax regime, approved investment
/// exemptions, LOP days) plus the <see cref="PayrollComponentLine"/>s every earlier stage has
/// already produced, via <see cref="Lines"/>/<see cref="AppendLines"/> — this is what lets, say,
/// the Statutory stage read the Earnings stage's Basic amount without knowing which class produced
/// it. The orchestrating <c>PayrollComputationEngine</c> (Vespera.Application) builds one of these
/// per employee and calls <see cref="AppendLines"/> after every rule; individual rules never mutate
/// it themselves — <c>IPayrollComponentRule.Apply</c> only reads it and returns new lines.
/// </summary>
public sealed class PayrollContext
{
    private readonly List<PayrollComponentLine> _lines = [];

    public PayrollContext(
        TenantId tenantId, PayrollRunId payrollRunId, EmployeeId employeeId, int month, int year, Currency currency,
        DateOnly periodStart, DateOnly periodEnd, DateOnly dateOfJoining, DateOnly? exitDate,
        IReadOnlyDictionary<SalaryComponentId, Money> resolvedSalaryComponents, IReadOnlyList<SalaryComponent> salaryComponents,
        decimal lossOfPayDays, IReadOnlyList<StatutoryRuleSet> statutoryRuleSets, TaxRegimeVersion? taxRegimeVersion,
        Money approvedInvestmentExemptions, IReadOnlyList<PayrollAdHocLine> voluntaryDeductions,
        IReadOnlyList<PayrollAdHocLine> reimbursements)
    {
        TenantId = tenantId;
        PayrollRunId = payrollRunId;
        EmployeeId = employeeId;
        Month = month;
        Year = year;
        Currency = currency;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        DateOfJoining = dateOfJoining;
        ExitDate = exitDate;
        ResolvedSalaryComponents = resolvedSalaryComponents;
        SalaryComponents = salaryComponents;
        LossOfPayDays = lossOfPayDays;
        StatutoryRuleSets = statutoryRuleSets;
        TaxRegimeVersion = taxRegimeVersion;
        ApprovedInvestmentExemptions = approvedInvestmentExemptions;
        VoluntaryDeductions = voluntaryDeductions;
        Reimbursements = reimbursements;
    }

    public TenantId TenantId { get; }

    public PayrollRunId PayrollRunId { get; }

    public EmployeeId EmployeeId { get; }

    public int Month { get; }

    public int Year { get; }

    public Currency Currency { get; }

    public DateOnly PeriodStart { get; }

    public DateOnly PeriodEnd { get; }

    public DateOnly DateOfJoining { get; }

    public DateOnly? ExitDate { get; }

    /// <summary>This employee's <see cref="SalaryStructure"/> formulas, already resolved to
    /// concrete monthly amounts (see <see cref="Services.SalaryStructureResolver"/>) — the
    /// CTC-down step happens before the pipeline runs, not inside it.</summary>
    public IReadOnlyDictionary<SalaryComponentId, Money> ResolvedSalaryComponents { get; }

    public IReadOnlyList<SalaryComponent> SalaryComponents { get; }

    public decimal LossOfPayDays { get; }

    public IReadOnlyList<StatutoryRuleSet> StatutoryRuleSets { get; }

    public TaxRegimeVersion? TaxRegimeVersion { get; }

    public Money ApprovedInvestmentExemptions { get; }

    public IReadOnlyList<PayrollAdHocLine> VoluntaryDeductions { get; }

    public IReadOnlyList<PayrollAdHocLine> Reimbursements { get; }

    public IReadOnlyList<PayrollComponentLine> Lines => _lines.AsReadOnly();

    public int DaysInPeriod => PeriodEnd.DayNumber - PeriodStart.DayNumber + 1;

    public void AppendLines(IEnumerable<PayrollComponentLine> lines) => _lines.AddRange(lines);

    public Money SumEarnings() =>
        _lines.Where(l => l.Direction == PayrollComponentDirection.Earning).Aggregate(Money.Zero(Currency), (total, l) => total + l.Amount);

    public Money SumDeductions() =>
        _lines.Where(l => l.Direction == PayrollComponentDirection.Deduction).Aggregate(Money.Zero(Currency), (total, l) => total + l.Amount);

    public Money? FindAmount(SalaryComponentId componentId) => _lines.FirstOrDefault(l => l.ComponentId == componentId)?.Amount;

    public StatutoryRuleSet? FindStatutoryRule(StatutoryRuleType ruleType) =>
        StatutoryRuleSets.FirstOrDefault(rule => rule.RuleType == ruleType);
}
