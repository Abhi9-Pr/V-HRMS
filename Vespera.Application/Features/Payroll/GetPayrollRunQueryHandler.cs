using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetPayrollRunQueryHandler : IRequestHandler<GetPayrollRunQuery, Result<PayrollRunDto>>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetPayrollRunQueryHandler(
        IReadRepository<PayrollRun> payrollRuns, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _payrollRuns = payrollRuns;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<PayrollRunDto>> Handle(GetPayrollRunQuery request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure<PayrollRunDto>(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        await _piiAccessAuditor.RecordAccessAsync(
            _tenantContext.TenantId, "PayrollRun", payrollRun.Id.Value, "Lines", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        return Result.Success(ToDto(payrollRun));
    }

    internal static PayrollRunDto ToDto(PayrollRun payrollRun) => new(
        payrollRun.Id.Value, payrollRun.Month, payrollRun.Year, payrollRun.Status.ToString(), payrollRun.DryRunExecutedBy,
        payrollRun.FreezeOverriddenBy, payrollRun.FreezeOverrideReason,
        [.. payrollRun.Lines.Select(line => new PayrollLineDto(
            line.EmployeeId.Value, line.Gross.Amount, line.Deductions.Amount, line.Net.Amount, line.LossOfPayDays))]);
}
