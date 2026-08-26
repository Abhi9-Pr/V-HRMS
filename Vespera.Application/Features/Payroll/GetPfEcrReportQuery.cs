using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record PfEcrReportLineDto(Guid EmployeeId, string EmployeeName, decimal WageBase, decimal EmployeeContribution);

/// <summary>
/// A representative Provident Fund ECR (Electronic Challan cum Return) extract — the commonly-
/// documented fields (member wages, employee contribution), not a certified match of EPFO's actual
/// current ECR file format, and not itemized to the exact LOP-adjusted wage the pipeline used at
/// dry-run time (that fine-grained detail isn't persisted on <c>PayrollRun.Lines</c>, only the
/// Gross/Deductions/Net rollup is — this report approximates the PF wage base as Gross). Verify
/// against the real EPFO spec, and reconcile line-by-line against the golden-file/payslip figures,
/// before ever filing this for real. ESI return, PT statement, and Form 16 data extract were not
/// implemented in this pass — see the plan's remaining-work notes.
/// </summary>
public sealed record GetPfEcrReportQuery(Guid PayrollRunId) : IRequest<Result<IReadOnlyList<PfEcrReportLineDto>>>;
