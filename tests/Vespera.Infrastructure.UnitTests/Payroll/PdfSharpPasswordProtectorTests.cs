using FluentAssertions;
using PdfSharpCore.Pdf.IO;
using QuestPDF.Infrastructure;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Payroll;

namespace Vespera.Infrastructure.UnitTests.Payroll;

public class PdfSharpPasswordProtectorTests
{
    static PdfSharpPasswordProtectorTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void Protect_Should_Require_A_Password_To_Reopen_The_Document()
    {
        var pdfBytes = RenderSamplePdf();
        var protector = new PdfSharpPasswordProtector();

        var protectedBytes = protector.Protect(pdfBytes, "AAAPZ1234A0105");

        using var stream = new MemoryStream(protectedBytes);
        var act = () => PdfReader.Open(stream, PdfDocumentOpenMode.InformationOnly);
        act.Should().Throw<Exception>("the reader needs the password before it can open the protected document");
    }

    [Fact]
    public void Protect_Should_Allow_Reopening_With_The_Correct_Password()
    {
        var pdfBytes = RenderSamplePdf();
        var protector = new PdfSharpPasswordProtector();
        const string password = "AAAPZ1234A0105";

        var protectedBytes = protector.Protect(pdfBytes, password);

        using var stream = new MemoryStream(protectedBytes);
        using var document = PdfReader.Open(stream, password, PdfDocumentOpenMode.InformationOnly);

        document.PageCount.Should().Be(1);
    }

    private static byte[] RenderSamplePdf()
    {
        var renderer = new QuestPdfPayslipRenderer();
        var request = new PayslipRenderRequest(
            "Vespera Technologies", "Priya Sharma", "EMP-001", "Software Engineer", "Engineering",
            5, 2026, "INR",
            [new PayslipRenderLine("Basic", 40000m)], [], 40000m, 0m, 40000m, 0m);
        return renderer.Render(request);
    }
}
