using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PublishPayrollRunCommandHandler : IRequestHandler<PublishPayrollRunCommand, Result>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishPayrollRunCommandHandler(IReadRepository<PayrollRun> payrollRuns, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PublishPayrollRunCommand request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        return payrollRun.Publish(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
