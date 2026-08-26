using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class SubmitPayrollRunForReviewCommandHandler : IRequestHandler<SubmitPayrollRunForReviewCommand, Result>
{
    private readonly IReadRepository<PayrollRun> _payrollRuns;

    public SubmitPayrollRunForReviewCommandHandler(IReadRepository<PayrollRun> payrollRuns) => _payrollRuns = payrollRuns;

    public async Task<Result> Handle(SubmitPayrollRunForReviewCommand request, CancellationToken cancellationToken)
    {
        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        return payrollRun.SubmitForReview();
    }
}
