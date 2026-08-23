using System.Text;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll.BankFiles;

/// <summary>SBI's commonly-documented fixed-width-style bulk salary-upload CSV field order.
/// Representative, not certified against SBI's current live specification — see <see cref="IBankFileFormatter"/>.</summary>
public sealed class SbiBankFileFormatter : IBankFileFormatter
{
    public string BankCode => "SBI";

    public BankFileExportResult Format(IReadOnlyList<BankTransferLine> lines)
    {
        var checksum = BankFileChecksumHelper.ComputeChecksum(lines);
        var total = BankFileChecksumHelper.TotalAmount(lines);

        var builder = new StringBuilder();
        builder.AppendLine("SlNo,BeneficiaryName,AccountNo,IFSC,Amount,Remarks");
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            builder.AppendLine(
                $"{index + 1},{BankFileChecksumHelper.Escape(line.EmployeeName)},{line.AccountNumber},{line.IfscCode},{line.Amount:F2},{BankFileChecksumHelper.Escape(line.Narration)}");
        }

        builder.AppendLine($"CONTROL,{lines.Count},{total:F2},{checksum}");

        return new BankFileExportResult(
            $"sbi-salary-transfer-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            builder.ToString(),
            BankFileChecksumHelper.BuildReconciliationReport(BankCode, lines, checksum));
    }
}
