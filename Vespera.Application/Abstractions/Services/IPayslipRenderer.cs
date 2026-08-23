namespace Vespera.Application.Abstractions.Services;

public sealed record PayslipRenderLine(string ComponentName, decimal Amount);

/// <summary>
/// Everything <see cref="IPayslipRenderer"/> needs to lay out one employee's payslip for one
/// period — a flat, Application-owned DTO rather than the Domain <c>Payslip</c>/<c>PayrollRun</c>
/// aggregates themselves, so the renderer (an Infrastructure concern) never depends on Domain.
/// </summary>
public sealed record PayslipRenderRequest(
    string CompanyName,
    string EmployeeName,
    string EmployeeCode,
    string Designation,
    string Department,
    int Month,
    int Year,
    string CurrencyCode,
    IReadOnlyList<PayslipRenderLine> EarningLines,
    IReadOnlyList<PayslipRenderLine> DeductionLines,
    decimal GrossPay,
    decimal TotalDeductions,
    decimal NetPay,
    decimal LossOfPayDays);

/// <summary>
/// Renders one payslip to PDF bytes. The layout is a conventional Indian payslip (header,
/// earnings/deductions tables, net pay, footer) invented for this build — <c>/docs/spec/vespera-master-spec.md</c>
/// has no real layout to match, documented plainly rather than claiming spec fidelity that doesn't exist.
/// </summary>
public interface IPayslipRenderer
{
    public byte[] Render(PayslipRenderRequest request);
}
