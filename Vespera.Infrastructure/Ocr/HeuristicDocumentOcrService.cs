using System.Text;
using System.Text.RegularExpressions;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Ocr;

/// <summary>
/// Dev/scaffold OCR implementation: treats the uploaded receipt as UTF-8 text and applies simple
/// regex heuristics to guess vendor/date/amount/tax. This is the seam for a real OCR vendor
/// (Azure Form Recognizer, Google Document AI, AWS Textract) — swap it out with one DI
/// registration line in <see cref="OcrServiceCollectionExtensions"/>; the <see cref="IDocumentOcrService"/>
/// contract never changes (OCP).
/// </summary>
public sealed partial class HeuristicDocumentOcrService : IDocumentOcrService
{
    public async Task<OcrResult> ExtractAsync(Stream document, CancellationToken cancellationToken)
    {
        string text;
        try
        {
            using var reader = new StreamReader(document, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            text = await reader.ReadToEndAsync(cancellationToken);
        }
        catch (IOException)
        {
            return new OcrResult(string.Empty, new Dictionary<string, string>(), 0.0);
        }
        catch (DecoderFallbackException)
        {
            return new OcrResult(string.Empty, new Dictionary<string, string>(), 0.0);
        }

        var fields = new Dictionary<string, string>();

        var vendorLine = text
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);
        if (vendorLine is not null)
        {
            fields["vendor"] = vendorLine;
        }

        var dateMatch = DatePattern().Match(text);
        if (dateMatch.Success)
        {
            fields["date"] = dateMatch.Value;
        }

        var amountMatch = AmountPattern().Match(text);
        if (amountMatch.Success)
        {
            fields["amount"] = amountMatch.Groups[1].Value;
        }

        var taxMatch = TaxPattern().Match(text);
        if (taxMatch.Success)
        {
            fields["tax"] = taxMatch.Groups[1].Value;
        }

        var confidence = fields.Count / 4.0;
        return new OcrResult(text, fields, confidence);
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4}")]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"(?i)(?:total|amount)\D{0,10}(\d+(?:\.\d{1,2})?)")]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"(?i)(?:tax|gst)\D{0,10}(\d+(?:\.\d{1,2})?)")]
    private static partial Regex TaxPattern();
}
