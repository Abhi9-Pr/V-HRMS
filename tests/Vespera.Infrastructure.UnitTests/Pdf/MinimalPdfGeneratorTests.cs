using System.Text;
using FluentAssertions;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Pdf;

namespace Vespera.Infrastructure.UnitTests.Pdf;

public class MinimalPdfGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Produce_Bytes_Starting_With_The_Pdf_Header_And_Ending_With_Eof()
    {
        var generator = new MinimalPdfGenerator();
        var request = new PdfGenerationRequest("OfferLetter", new Dictionary<string, object?> { ["Candidate"] = "Jordan Lee" });

        var bytes = await generator.GenerateAsync(request, CancellationToken.None);
        var text = Encoding.ASCII.GetString(bytes);

        text.Should().StartWith("%PDF-1.");
        text.TrimEnd().Should().EndWith("%%EOF");
    }

    [Fact]
    public async Task GenerateAsync_Should_Produce_A_Structurally_Valid_Xref_Table()
    {
        var generator = new MinimalPdfGenerator();
        var request = new PdfGenerationRequest(
            "OfferLetter",
            new Dictionary<string, object?> { ["Candidate"] = "Jordan Lee", ["Proposed CTC"] = "1200000 Inr", ["Joining Date"] = "2026-03-01" });

        var bytes = await generator.GenerateAsync(request, CancellationToken.None);
        var text = Encoding.ASCII.GetString(bytes);

        // Parse "startxref\n<offset>\n%%EOF" back out and confirm it points at a real "xref" line.
        var startXrefIndex = text.LastIndexOf("startxref", StringComparison.Ordinal);
        startXrefIndex.Should().BeGreaterThan(0);

        var afterStartXref = text[(startXrefIndex + "startxref\n".Length)..];
        var xrefOffsetText = afterStartXref[..afterStartXref.IndexOf('\n')];
        var xrefOffset = int.Parse(xrefOffsetText);

        text.Substring(xrefOffset, 4).Should().Be("xref");

        // Every "nnnnnnnnnn 00000 n " entry in the xref table must point at the start of a real
        // "<n> 0 obj" — this is the check that actually catches a broken table (a PDF with the
        // right header/footer but garbage offsets still "looks" valid at a glance).
        var xrefBlock = text[xrefOffset..text.IndexOf("trailer", xrefOffset, StringComparison.Ordinal)];
        var entryLines = xrefBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2).ToList(); // skip "xref" and "0 N" header, and the free-list entry is line[0] here
        var objectEntries = entryLines.Skip(1); // first real line after the free-list "0000000000 65535 f " entry

        var objectNumber = 1;
        foreach (var entry in objectEntries)
        {
            var offset = int.Parse(entry.Split(' ')[0]);
            text.Substring(offset, $"{objectNumber} 0 obj".Length).Should().Be($"{objectNumber} 0 obj");
            objectNumber++;
        }
    }
}
