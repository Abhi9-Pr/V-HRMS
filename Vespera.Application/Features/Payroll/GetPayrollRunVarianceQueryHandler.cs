using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Diffs this run's lines against the most recent prior Finalized/Published run, flagging anything
/// worth a human's attention before approval: a net that moved more than
/// <see cref="PayrollSettings.VarianceThresholdPercent"/>, an employee with no prior line (new
/// joiner) or no current line (exit), or a zero net pay. If there's no prior run at all (this
/// tenant's first payroll cycle), every current line is reported with no prior/variance data and no
/// flags — there's nothing to compare against yet.
/// </summary>
public sealed class GetPayrollRunVarianceQueryHandler : IRequestHandler<GetPayrollRunVarianceQuery, Result<IReadOnlyList<PayrollVarianceLine>>>
{
    private const decimal DefaultVarianceThresholdPercent = 20m;

    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<PayrollSettings> _payrollSettings;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetPayrollRunVarianceQueryHandler(
        IReadRepository<PayrollRun> payrollRuns, IReadRepository<PayrollSettings> payrollSettings, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _payrollRuns = payrollRuns;
        _payrollSettings = payrollSettings;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<IReadOnlyList<PayrollVarianceLine>>> Handle(
        GetPayrollRunVarianceQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var payrollRunId = new PayrollRunId(request.PayrollRunId);

        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(new PayrollRunByIdSpecification(payrollRunId), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure<IReadOnlyList<PayrollVarianceLine>>(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        var priorRun = await _payrollRuns.FirstOrDefaultAsync(
            new PriorFinalizedPayrollRunSpecification(tenantId, payrollRunId), cancellationToken);
        var settings = await _payrollSettings.FirstOrDefaultAsync(new PayrollSettingsByTenantSpecification(tenantId), cancellationToken);
        var thresholdPercent = settings?.VarianceThresholdPercent ?? DefaultVarianceThresholdPercent;

        var priorNetByEmployee = (priorRun?.Lines ?? []).ToDictionary(line => line.EmployeeId.Value, line => line.Net.Amount);
        var currentNetByEmployee = payrollRun.Lines.ToDictionary(line => line.EmployeeId.Value, line => line.Net.Amount);

        var results = new List<PayrollVarianceLine>();

        foreach (var (employeeId, currentNet) in currentNetByEmployee)
        {
            var flags = new List<PayrollVarianceFlag>();
            decimal? variancePercent = null;

            if (priorNetByEmployee.TryGetValue(employeeId, out var priorNet))
            {
                if (priorNet != 0)
                {
                    variancePercent = Math.Abs(currentNet - priorNet) / Math.Abs(priorNet) * 100m;
                    if (variancePercent > thresholdPercent)
                    {
                        flags.Add(PayrollVarianceFlag.LargeVariance);
                    }
                }
            }
            else if (priorRun is not null)
            {
                flags.Add(PayrollVarianceFlag.NewJoiner);
            }

            if (currentNet == 0)
            {
                flags.Add(PayrollVarianceFlag.ZeroNet);
            }

            results.Add(new PayrollVarianceLine(
                employeeId, priorNetByEmployee.TryGetValue(employeeId, out var prior) ? prior : null, currentNet, variancePercent, flags));
        }

        if (priorRun is not null)
        {
            foreach (var employeeId in priorNetByEmployee.Keys.Except(currentNetByEmployee.Keys))
            {
                results.Add(new PayrollVarianceLine(employeeId, priorNetByEmployee[employeeId], null, null, [PayrollVarianceFlag.Exit]));
            }
        }

        var now = _dateTimeProvider.UtcNow;
        var accessedBy = _currentUser.UserId?.ToString() ?? "system";
        await _piiAccessAuditor.RecordAccessAsync(
            tenantId, "PayrollRun", payrollRun.Id.Value, "Variance", accessedBy, now, cancellationToken);

        return Result.Success<IReadOnlyList<PayrollVarianceLine>>(results);
    }
}
