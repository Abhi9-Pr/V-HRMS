using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetPfEcrReportQueryHandler : IRequestHandler<GetPfEcrReportQuery, Result<IReadOnlyList<PfEcrReportLineDto>>>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets;
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetPfEcrReportQueryHandler(
        IReadRepository<PayrollRun> payrollRuns, IReadRepository<StatutoryRuleSet> statutoryRuleSets, IReadRepository<Employee> employees,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _payrollRuns = payrollRuns;
        _statutoryRuleSets = statutoryRuleSets;
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<IReadOnlyList<PfEcrReportLineDto>>> Handle(GetPfEcrReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var payrollRunId = new PayrollRunId(request.PayrollRunId);

        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(new PayrollRunByIdSpecification(payrollRunId), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure<IReadOnlyList<PfEcrReportLineDto>>(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        var periodStart = new DateOnly(payrollRun.Year, payrollRun.Month, 1);
        var pfRule = (await _statutoryRuleSets.ListAsync(
                new StatutoryRuleSetsActiveOnDateSpecification(tenantId, periodStart), cancellationToken))
            .FirstOrDefault(rule => rule.RuleType == StatutoryRuleType.ProvidentFund);
        if (pfRule is null)
        {
            return Result.Failure<IReadOnlyList<PfEcrReportLineDto>>(Error.NotFound(
                "pf_ecr_report.no_rule", "No Provident Fund statutory rule is configured for this period."));
        }

        var lines = new List<PfEcrReportLineDto>();
        foreach (var payLine in payrollRun.Lines)
        {
            var employee = await _employees.FirstOrDefaultAsync(
                new EmployeeByIdSpecification(tenantId, payLine.EmployeeId), cancellationToken);
            var contribution = Math.Round(payLine.Gross.Amount * pfRule.RatePercent / 100m, 2);
            if (pfRule.CapAmount is not null)
            {
                contribution = Math.Min(contribution, pfRule.CapAmount.Amount);
            }

            lines.Add(new PfEcrReportLineDto(
                payLine.EmployeeId.Value, employee is null ? "Unknown" : $"{employee.FirstName} {employee.LastName}",
                payLine.Gross.Amount, contribution));
        }

        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "PayrollRun", payrollRun.Id.Value, "PfEcrReport", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        return Result.Success<IReadOnlyList<PfEcrReportLineDto>>(lines);
    }
}
