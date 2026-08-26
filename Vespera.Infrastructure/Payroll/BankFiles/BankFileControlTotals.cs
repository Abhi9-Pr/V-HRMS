using System.Security.Cryptography;
using System.Text;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Payroll.BankFiles;

/// <summary>Shared control-total/checksum and reconciliation-report logic for every
/// <see cref="IBankFileFormatter"/> — each bank still owns its own field layout, this only removes
/// the duplication in the trailer/report boilerplate every formatter needs regardless.</summary>
internal static class BankFileControlTotals
{
    public static decimal TotalAmount(IReadOnlyList<BankTransferLine> lines) => lines.Sum(line => line.Amount);

    /// <summary>A short content digest over account number + amount pairs, so a corrupted or
    /// reordered file (not just a wrong total) is detectable — combined with the record count and
    /// total amount in the trailer record for a belt-and-braces control total.</summary>
    public static string ComputeChecksum(IReadOnlyList<BankTransferLine> lines)
    {
        var payload = string.Join('|', lines.Select(line => $"{line.AccountNumber}:{line.Amount:F2}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash)[..16];
    }

    public static string Escape(string value) => value.Contains(',') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    public static string BuildReconciliationReport(string bankCode, IReadOnlyList<BankTransferLine> lines, string checksum)
    {
        var total = TotalAmount(lines);
        var builder = new StringBuilder();
        builder.AppendLine($"Bank: {bankCode}");
        builder.AppendLine($"Record count: {lines.Count}");
        builder.AppendLine($"Total amount: {total:F2}");
        builder.AppendLine($"Checksum: {checksum}");
        builder.AppendLine();
        builder.AppendLine("Employee,Account,Amount");
        foreach (var line in lines)
        {
            builder.AppendLine($"{Escape(line.EmployeeName)},{line.AccountNumber},{line.Amount:F2}");
        }

        return builder.ToString();
    }
}
