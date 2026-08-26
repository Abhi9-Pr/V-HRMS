using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class FreezeAttendanceCommandHandler : IRequestHandler<FreezeAttendanceCommand, Result>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<PayrollSettings> _payrollSettings;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FreezeAttendanceCommandHandler(
        IReadRepository<PayrollRun> payrollRuns, IReadRepository<PayrollSettings> payrollSettings,
        ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _payrollSettings = payrollSettings;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(FreezeAttendanceCommand request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        var settings = await _payrollSettings.FirstOrDefaultAsync(
            new PayrollSettingsByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var freezeDay = settings?.AttendanceFreezeDay ?? 25;

        var occurredOn = _dateTimeProvider.UtcNow;
        return payrollRun.FreezeAttendance(
            DateOnly.FromDateTime(occurredOn.UtcDateTime), freezeDay, occurredOn,
            _currentUser.UserId?.ToString() ?? "system", request.OverrideReason);
    }
}
