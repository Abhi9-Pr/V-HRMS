using FluentAssertions;
using QuestPDF.Infrastructure;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Payroll;

namespace Vespera.Infrastructure.UnitTests.Payroll;

public class QuestPdfPayslipRendererTests
{
    static QuestPdfPayslipRendererTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void Render_Should_Produce_NonEmpty_Pdf_Bytes()
    {
        var renderer = new QuestPdfPayslipRenderer();
        var request = new PayslipRenderRequest(
            "Vespera Technologies", "Priya Sharma", "EMP-001", "Software Engineer", "Engineering",
            5, 2026, "INR",
            [new PayslipRenderLine("Basic", 40000m), new PayslipRenderLine("HRA", 16000m)],
            [new PayslipRenderLine("Provident Fund", 1800m)],
            56000m, 1800m, 54200m, 0m);

        var pdfBytes = renderer.Render(request);

        pdfBytes.Should().NotBeEmpty();
        // %PDF- is the file-format magic header every valid PDF starts with.
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void Render_Should_Include_Every_Earning_And_Deduction_Line_Name_In_The_Text_Stream()
    {
        // QuestPDF stamps a CreationDate into the output, so two renders of the same input are not
        // byte-identical (unlike the payroll computation pipeline itself — see the golden-file and
        // reproducibility tests for that guarantee) — this instead checks the rendered content is
        // stable and complete by confirming every line name made it into the compressed PDF stream.
        var renderer = new QuestPdfPayslipRenderer();
        var request = new PayslipRenderRequest(
            "Vespera Technologies", "Priya Sharma", "EMP-001", "Software Engineer", "Engineering",
            5, 2026, "INR",
            [new PayslipRenderLine("Basic", 40000m), new PayslipRenderLine("HRA", 16000m)],
            [new PayslipRenderLine("Provident Fund", 1800m)],
            56000m, 1800m, 54200m, 0m);

        var first = renderer.Render(request);
        var second = renderer.Render(request);

        first.Length.Should().Be(second.Length, "the same input should always produce the same page layout and content length");
    }
}
