using FluentAssertions;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Payroll.BankFiles;

namespace Vespera.Infrastructure.UnitTests.Payroll;

public class BankFileFormatterTests
{
    private static readonly BankTransferLine[] Lines =
    [
        new("Priya Sharma", "000123456789", "ICIC0000123", 72000m, "Salary May 2026"),
        new("Rohan Verma", "000987654321", "ICIC0000456", 65000m, "Salary May 2026"),
    ];

    [Theory]
    [InlineData(typeof(IciciBankFileFormatter), "ICICI")]
    [InlineData(typeof(HdfcBankFileFormatter), "HDFC")]
    [InlineData(typeof(SbiBankFileFormatter), "SBI")]
    public void Format_Should_Report_The_Correct_Bank_Code(Type formatterType, string expectedBankCode)
    {
        var formatter = (IBankFileFormatter)Activator.CreateInstance(formatterType)!;

        formatter.BankCode.Should().Be(expectedBankCode);
    }

    [Fact]
    public void IciciFormatter_Should_Include_Every_Line_And_A_Control_Total()
    {
        var formatter = new IciciBankFileFormatter();

        var result = formatter.Format(Lines);

        result.FileContent.Should().Contain("Priya Sharma").And.Contain("Rohan Verma");
        result.FileContent.Should().Contain("TOTAL_AMOUNT=137000.00");
        result.ReconciliationReport.Should().Contain("Record count: 2").And.Contain("Total amount: 137000.00");
    }

    [Fact]
    public void HdfcFormatter_Should_Include_Every_Line_And_A_Control_Total()
    {
        var formatter = new HdfcBankFileFormatter();

        var result = formatter.Format(Lines);

        result.FileContent.Should().Contain("Priya Sharma").And.Contain("Rohan Verma");
        result.FileContent.Should().StartWith("H|");
        result.FileContent.Should().Contain("T|2|137000.00|");
    }

    [Fact]
    public void SbiFormatter_Should_Include_Every_Line_And_A_Control_Total()
    {
        var formatter = new SbiBankFileFormatter();

        var result = formatter.Format(Lines);

        result.FileContent.Should().Contain("Priya Sharma").And.Contain("Rohan Verma");
        result.FileContent.Should().Contain("CONTROL,2,137000.00,");
    }

    [Fact]
    public void Different_Banks_Should_Produce_The_Same_Checksum_For_The_Same_Lines()
    {
        // The checksum only depends on account numbers and amounts, not the bank-specific layout —
        // useful for cross-checking a reconciliation report against a different bank's export of
        // the same run.
        var icici = new IciciBankFileFormatter().Format(Lines);
        var sbi = new SbiBankFileFormatter().Format(Lines);

        var iciciChecksum = ExtractAfter(icici.FileContent, "CHECKSUM=");
        var sbiChecksum = ExtractAfter(sbi.ReconciliationReport, "Checksum: ");

        iciciChecksum.Should().Be(sbiChecksum);
    }

    private static string ExtractAfter(string content, string marker)
    {
        var index = content.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var remainder = content[index..];
        var end = remainder.IndexOfAny(['\r', '\n']);
        return end >= 0 ? remainder[..end] : remainder;
    }
}
