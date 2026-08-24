using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class OpenPayrollRunCommandHandler : IRequestHandler<OpenPayrollRunCommand, Result<Guid>>
{
    private readonly IWriteRepository<PayrollRun> _payrollRuns;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OpenPayrollRunCommandHandler(
        IWriteRepository<PayrollRun> payrollRuns, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(OpenPayrollRunCommand request, CancellationToken cancellationToken)
    {
        var result = PayrollRun.Open(
            _tenantContext.TenantId, request.Month, request.Year, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _payrollRuns.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
