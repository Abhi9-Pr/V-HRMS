using FluentValidation;

namespace Vespera.Application.Features.Employees.Import;

/// <summary>
/// Deliberately shallow — only rejects a genuinely malformed request (no rows at all, or a
/// non-positive row number, which could only come from a broken parser upstream). Per-row
/// business validation (format, duplicate codes, unresolvable department/designation/location)
/// is NOT done here: those are the literal output this command produces (the row-level dry-run
/// report), not a gate that should abort the whole request via the pipeline's
/// <c>ValidationBehavior</c> — one bad row in a 500-row import must still surface a report
/// listing all 500 rows, not a blanket 400.
/// </summary>
public sealed class ImportEmployeesCommandValidator : AbstractValidator<ImportEmployeesCommand>
{
    public ImportEmployeesCommandValidator()
    {
        RuleFor(command => command.Rows).NotEmpty();
        RuleForEach(command => command.Rows).ChildRules(row =>
        {
            row.RuleFor(r => r.RowNumber).GreaterThan(0);
        });
    }
}
