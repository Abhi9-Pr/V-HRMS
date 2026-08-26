using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>
/// Selects the right <see cref="IBankFileFormatter"/> by <see cref="ExportBankFileCommand.BankCode"/>
/// from every registered formatter — adding a bank is one new class and one DI registration line,
/// nothing here changes. IFSC is not yet a captured Employee field in this codebase (only account
/// number is), so every line's IFSC is a placeholder — a real gap, not fabricated data, flagged so
/// it's obvious in the exported file rather than silently wrong.
/// </summary>
public sealed class ExportBankFileCommandHandler : IRequestHandler<ExportBankFileCommand, Result<BankFileExportResult>>
{
    private const string IfscNotCaptured = "IFSC-NOT-CAPTURED";

    private readonly IReadRepository<PayrollRun> _payrollRuns;
    private readonly IReadRepository<Employee> _employees;
    private readonly IEnumerable<IBankFileFormatter> _formatters;

    public ExportBankFileCommandHandler(IReadRepository<PayrollRun> payrollRuns, IReadRepository<Employee> employees, IEnumerable<IBankFileFormatter> formatters)
    {
        _payrollRuns = payrollRuns;
        _employees = employees;
        _formatters = formatters;
    }

    public async Task<Result<BankFileExportResult>> Handle(ExportBankFileCommand request, CancellationToken cancellationToken)
    {
        var formatter = _formatters.FirstOrDefault(f => string.Equals(f.BankCode, request.BankCode, StringComparison.OrdinalIgnoreCase));
        if (formatter is null)
        {
            return Result.Failure<BankFileExportResult>(Error.Validation("bank_export.unknown_bank", $"No formatter registered for bank code '{request.BankCode}'."));
        }

        var payrollRun = await _payrollRuns.FirstOrDefaultAsync(
            new PayrollRunByIdSpecification(new PayrollRunId(request.PayrollRunId)), cancellationToken);
        if (payrollRun is null)
        {
            return Result.Failure<BankFileExportResult>(Error.NotFound("payroll_run.not_found", "Payroll run not found."));
        }

        if (payrollRun.Status is not (PayrollRunStatus.Finalized or PayrollRunStatus.Published))
        {
            return Result.Failure<BankFileExportResult>(Error.Conflict(
                "bank_export.run_not_finalized", "Bank files can only be exported for a finalized payroll run."));
        }

        var lines = new List<BankTransferLine>();
        foreach (var payLine in payrollRun.Lines)
        {
            var employee = await _employees.FirstOrDefaultAsync(
                new EmployeeByIdSpecification(payrollRun.TenantId, payLine.EmployeeId), cancellationToken);
            if (employee?.BankAccount is null)
            {
                continue;
            }

            lines.Add(new BankTransferLine(
                $"{employee.FirstName} {employee.LastName}", employee.BankAccount.Value, IfscNotCaptured, payLine.Net.Amount,
                $"Salary {payrollRun.Month:D2}/{payrollRun.Year}"));
        }

        if (lines.Count == 0)
        {
            return Result.Failure<BankFileExportResult>(Error.Validation(
                "bank_export.no_bank_details", "No employee on this run has a bank account on record."));
        }

        return Result.Success(formatter.Format(lines));
    }
}
