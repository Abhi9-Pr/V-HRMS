using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Import;

public sealed record ImportEmployeesCommand(
    IReadOnlyList<ImportEmployeeRowDto> Rows,
    bool DryRun) : IRequest<Result<BulkImportReportDto>>;

public sealed record BulkImportRowResult(int RowNumber, bool Success, IReadOnlyList<string> Errors);

/// <summary>
/// <see cref="Committed"/> is true only when this was a real (non-dry-run) run where every row
/// passed — a real run with any row-level failure reports the same per-row detail but creates
/// nothing (all-or-nothing), and a dry run never creates anything regardless of outcome.
/// </summary>
public sealed record BulkImportReportDto(
    IReadOnlyList<BulkImportRowResult> Rows,
    int SuccessCount,
    int FailureCount,
    bool Committed);
