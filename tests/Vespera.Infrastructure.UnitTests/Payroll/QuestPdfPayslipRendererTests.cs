using FluentAssertions;
using PdfSharpCore.Pdf.IO;
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
    public void Render_Should_Produce_A_Single_Page_Regardless_Of_Line_Count()
    {
        // QuestPDF stamps a CreationDate into the output, so two renders of the same input are not
        // byte-identical (unlike the payroll computation pipeline itself — see the golden-file and
        // reproducibility tests for that guarantee). This checks structure instead of bytes: a
        // typical payslip's earnings/deductions fit on one page.
        var renderer = new QuestPdfPayslipRenderer();
        var request = new PayslipRenderRequest(
            "Vespera Technologies", "Priya Sharma", "EMP-001", "Software Engineer", "Engineering",
            5, 2026, "INR",
            [new PayslipRenderLine("Basic", 40000m), new PayslipRenderLine("HRA", 16000m)],
            [new PayslipRenderLine("Provident Fund", 1800m)],
            56000m, 1800m, 54200m, 0m);

        var pdfBytes = renderer.Render(request);

        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
        document.PageCount.Should().Be(1);
    }
}
