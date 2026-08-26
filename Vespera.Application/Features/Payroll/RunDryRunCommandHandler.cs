using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Builds one <see cref="PayrollContext"/> per employee with an active <see cref="SalaryStructure"/>
/// for the run's period, runs each through <see cref="PayrollComputationEngine"/>, and hands the
/// rolled-up totals to <see cref="PayrollRun.RecomputeLines"/>. This is the "caller" the pipeline's
/// own tests describe as responsible for loading input data — repository I/O lives here, not in
/// any rule. Voluntary deductions and reimbursement claims are deliberately not modelled as their
/// own aggregates yet (see <c>PayrollAdHocLine</c>'s remarks), so every context is built with empty
/// lists for both — a future CQRS slice can populate them without touching this handler's shape.
/// </summary>
public sealed class RunDryRunCommandHandler : IRequestHandler<RunDryRunCommand, Result>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<SalaryStructure> _salaryStructures;
    private readonly IReadRepository<SalaryComponent> _salaryComponents;
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets;
    private readonly IReadRepository<InvestmentDeclaration> _investmentDeclarations;
    private readonly IReadRepository<TaxRegimeVersion> _taxRegimeVersions;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly IReadRepository<Employee> _employees;
    private readonly PayrollComputationEngine _engine;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RunDryRunCommandHandler(
        IReadRepository<PayrollRun> payrollRuns, IReadRepository<SalaryStructure> salaryStructures,
        IReadRepository<SalaryComponent> salaryComponents, IReadRepository<StatutoryRuleSet> statutoryRuleSets,
        IReadRepository<InvestmentDeclaration> investmentDeclarations, IReadRepository<TaxRegimeVersion> taxRegimeVersions,
        IReadRepository<LeaveRequest> leaveRequests, IReadRepository<Employee> employees, PayrollComputationEngine engine,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _salaryStructures = salaryStructures;
        _salaryComponents = salaryComponents;
        _statutoryRuleSets = statutoryRuleSets;
        _investmentDeclarations = investmentDeclarations;
        _taxRegimeVersions = taxRegimeVersions;
        _leaveRequests = leaveRequests;
        _employees = employees;
        _engine = engine;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RunDryRunCommand request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        var tenantId = _tenantContext.TenantId;
        var periodStart = new DateOnly(payrollRun.Year, payrollRun.Month, 1);
        var periodEnd = new DateOnly(payrollRun.Year, payrollRun.Month, DateTime.DaysInMonth(payrollRun.Year, payrollRun.Month));
        var financialYear = ToFinancialYear(payrollRun.Month, payrollRun.Year);

        var salaryStructures = await _salaryStructures.ListAsync(
            new SalaryStructuresActiveOnDateSpecification(tenantId, periodStart), cancellationToken);
        if (salaryStructures.Count == 0)
        {
            return Result.Failure(Error.Validation(
                "payroll_run.no_employees", "No employee has an active salary structure for this period."));
        }

        var salaryComponents = await _salaryComponents.ListAsync(new AllSalaryComponentsSpecification(tenantId), cancellationToken);
        var statutoryRuleSets = await _statutoryRuleSets.ListAsync(
            new StatutoryRuleSetsActiveOnDateSpecification(tenantId, periodStart), cancellationToken);

        var lineInputs = new List<PayrollLineInput>();
        foreach (var structure in salaryStructures)
        {
            var resolved = SalaryStructureResolver.ResolveMonthly(structure);
            if (resolved.IsFailure)
            {
                return Result.Failure(resolved.Error);
            }

            var employee = await _employees.FirstOrDefaultAsync(
                new EmployeeByIdSpecification(tenantId, structure.EmployeeId), cancellationToken);
            if (employee is null)
            {
                continue;
            }

            var lopRequests = await _leaveRequests.ListAsync(
                new ApprovedLopLeaveRequestsOverlappingPeriodSpecification(tenantId, structure.EmployeeId, periodStart, periodEnd),
                cancellationToken);
            var lossOfPayDays = lopRequests.Sum(leaveRequest => leaveRequest.LossOfPayDays);

            var declaration = await _investmentDeclarations.FirstOrDefaultAsync(
                new VerifiedInvestmentDeclarationSpecification(tenantId, structure.EmployeeId, financialYear), cancellationToken);

            TaxRegimeVersion? regime = null;
            var approvedExemptions = Money.Zero(structure.MonthlyCtc.Currency);
            if (declaration is not null)
            {
                regime = await _taxRegimeVersions.FirstOrDefaultAsync(
                    new TaxRegimeVersionByIdSpecification(declaration.TaxRegimeVersionId), cancellationToken);
                approvedExemptions = declaration.ApprovedExemptionTotal(structure.MonthlyCtc.Currency);
            }

            var context = new PayrollContext(
                tenantId, payrollRun.Id, structure.EmployeeId, payrollRun.Month, payrollRun.Year, structure.MonthlyCtc.Currency,
                periodStart, periodEnd, employee.DateOfJoining, employee.ExitDate,
                resolved.Value, salaryComponents, lossOfPayDays, statutoryRuleSets, regime, approvedExemptions, [], []);

            lineInputs.Add(_engine.ComputeForEmployee(context));
        }

        return payrollRun.RecomputeLines(lineInputs, _currentUser.UserId?.ToString() ?? "system", _dateTimeProvider.UtcNow);
    }

    /// <summary>Indian financial year: April-March. Month 4-12 of <paramref name="year"/> belongs
    /// to FY "{year}-{year+1}"; month 1-3 belongs to FY "{year-1}-{year}".</summary>
    private static string ToFinancialYear(int month, int year) =>
        month >= 4 ? $"{year}-{(year + 1) % 100:D2}" : $"{year - 1}-{year % 100:D2}";
}
