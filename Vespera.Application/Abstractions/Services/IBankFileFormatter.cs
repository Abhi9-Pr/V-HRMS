namespace Vespera.Application.Abstractions.Services;

public sealed record BankTransferLine(string EmployeeName, string AccountNumber, string IfscCode, decimal Amount, string Narration);

public sealed record BankFileExportResult(string FileName, string FileContent, string ReconciliationReport);

/// <summary>
/// One bank's bulk salary-transfer file format. Selected at runtime by <see cref="BankCode"/> from
/// <c>IEnumerable&lt;IBankFileFormatter&gt;</c> — adding a bank means writing one new class and one
/// DI registration line, the same OCP shape as <c>IPayrollComponentRule</c>. Field layouts here are
/// representative — the commonly-documented bulk-transfer field sets (account number, IFSC,
/// beneficiary, amount, narration) plus a checksum/control-total trailer record — not certified
/// against any bank's current live specification; verify against the actual spec before a real run.
/// </summary>
public interface IBankFileFormatter
{
    public string BankCode { get; }

    public BankFileExportResult Format(IReadOnlyList<BankTransferLine> lines);
}
