using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Maker-checker's "checker" side (the "not the creator" check itself lives in Api's
/// NotCreatorAuthorizationHandler, applied by the controller before this command is dispatched —
/// a command has no notion of HTTP authorization, per the pattern established for
/// RegisterUserCommand's admin-only check).
/// </summary>
public sealed class FinalizePayrollRunCommandHandler : IRequestHandler<FinalizePayrollRunCommand, Result>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FinalizePayrollRunCommandHandler(IReadRepository<PayrollRun> payrollRuns, IDateTimeProvider dateTimeProvider)
    {
        _payrollRuns = payrollRuns;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(FinalizePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);

        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        return payrollRun.Finalize(_dateTimeProvider.UtcNow);
    }
}
