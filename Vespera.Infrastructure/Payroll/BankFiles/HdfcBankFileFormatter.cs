using System.Text;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll.BankFiles;

/// <summary>HDFC's commonly-documented pipe-delimited bulk salary-upload field order.
/// Representative, not certified against HDFC's current live specification — see <see cref="IBankFileFormatter"/>.</summary>
public sealed class HdfcBankFileFormatter : IBankFileFormatter
{
    public string BankCode => "HDFC";

    public BankFileExportResult Format(IReadOnlyList<BankTransferLine> lines)
    {
        var checksum = BankFileControlTotals.ComputeChecksum(lines);
        var total = BankFileControlTotals.TotalAmount(lines);

        var builder = new StringBuilder();
        builder.AppendLine("H|BENE_NAME|BENE_ACCT_NO|BENE_IFSC|TXN_AMOUNT|REMARKS");
        foreach (var line in lines)
        {
            builder.AppendLine($"D|{line.EmployeeName}|{line.AccountNumber}|{line.IfscCode}|{line.Amount:F2}|{line.Narration}");
        }

        builder.AppendLine($"T|{lines.Count}|{total:F2}|{checksum}");

        return new BankFileExportResult(
            $"hdfc-salary-transfer-{DateTime.UtcNow:yyyyMMddHHmmss}.txt",
            builder.ToString(),
            BankFileControlTotals.BuildReconciliationReport(BankCode, lines, checksum));
    }
}
