using System.Text;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll.BankFiles;

/// <summary>ICICI's commonly-documented bulk salary-upload CSV field order. Representative, not
/// certified against ICICI's current live specification — see <see cref="IBankFileFormatter"/>.</summary>
public sealed class IciciBankFileFormatter : IBankFileFormatter
{
    public string BankCode => "ICICI";

    public BankFileExportResult Format(IReadOnlyList<BankTransferLine> lines)
    {
        var checksum = BankFileChecksumHelper.ComputeChecksum(lines);
        var total = BankFileChecksumHelper.TotalAmount(lines);

        var builder = new StringBuilder();
        builder.AppendLine("BENEFICIARY_NAME,ACCOUNT_NUMBER,IFSC_CODE,AMOUNT,NARRATION");
        foreach (var line in lines)
        {
            builder.AppendLine(
                $"{BankFileChecksumHelper.Escape(line.EmployeeName)},{line.AccountNumber},{line.IfscCode},{line.Amount:F2},{BankFileChecksumHelper.Escape(line.Narration)}");
        }

        builder.AppendLine($"TRAILER,RECORD_COUNT={lines.Count},TOTAL_AMOUNT={total:F2},CHECKSUM={checksum}");

        return new BankFileExportResult(
            $"icici-salary-transfer-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            builder.ToString(),
            BankFileChecksumHelper.BuildReconciliationReport(BankCode, lines, checksum));
    }
}
