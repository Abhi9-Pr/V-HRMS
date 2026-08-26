using System.Text;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Pdf;

/// <summary>
/// Hand-emits a minimal, valid single-page PDF 1.4 document — a title heading (the
/// <see cref="PdfGenerationRequest.TemplateKey"/>) plus one "Key: Value" line per
/// <see cref="PdfGenerationRequest.Data"/> entry, rendered in the built-in Helvetica base-14 font
/// (no font embedding needed). This is a dev-grade, dependency-free seam: no PDF library
/// (QuestPDF/iText/PdfSharp) is referenced anywhere in this codebase, and adding one is a bigger
/// dependency decision than this feature warrants. Swap this for a real templating library later
/// via one DI registration line — see <c>HeuristicDocumentOcrService</c> for the same pattern
/// applied to OCR. Single page only: content beyond <see cref="MaxLines"/> is truncated, not
/// wrapped or paginated.
/// </summary>
public sealed class MinimalPdfGenerator : IPdfGenerator
{
    private const int MaxLines = 40;

    public Task<byte[]> GenerateAsync(PdfGenerationRequest request, CancellationToken cancellationToken)
    {
        var lines = new List<string> { request.TemplateKey };
        lines.AddRange(request.Data.Select(entry => $"{entry.Key}: {entry.Value}"));

        var contentStream = BuildContentStream(lines.Take(MaxLines).ToList());
        return Task.FromResult(BuildPdf(contentStream));
    }

    private static string BuildContentStream(List<string> lines)
    {
        var stream = new StringBuilder();
        stream.Append("BT /F1 12 Tf 50 740 Td\n");

        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                stream.Append("0 -16 Td\n");
            }

            stream.Append('(').Append(Escape(lines[i])).Append(") Tj\n");
        }

        stream.Append("ET");
        return stream.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    /// <summary>
    /// Builds a classic (non-cross-reference-stream) PDF: five numbered objects (Catalog, Pages,
    /// Page, Font, Content stream), a byte-accurate xref table, and a trailer. Offsets are tracked
    /// via <see cref="StringBuilder.Length"/> as the document is assembled — safe because every
    /// character emitted here is ASCII, where <see cref="Encoding.ASCII"/> always maps one char to
    /// exactly one byte (even a stray non-ASCII character in candidate-supplied data becomes a
    /// single '?' byte, never a variable-width sequence), so char count and byte count never
    /// diverge and the xref offsets stay correct.
    /// </summary>
    private static byte[] BuildPdf(string contentStream)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 612 792] /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentStream.Length} >>\nstream\n{contentStream}\nendstream",
        };

        var buffer = new StringBuilder();
        buffer.Append("%PDF-1.4\n");

        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(buffer.Length);
            buffer.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = buffer.Length;
        buffer.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        buffer.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            buffer.Append(offset.ToString("D10")).Append(" 00000 n \n");
        }

        buffer.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        buffer.Append("startxref\n").Append(xrefOffset).Append('\n');
        buffer.Append("%%EOF");

        return Encoding.ASCII.GetBytes(buffer.ToString());
    }
}
