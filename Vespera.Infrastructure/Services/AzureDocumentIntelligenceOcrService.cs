using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Services;

/// <summary>
/// Cloud OCR via Azure AI Document Intelligence's prebuilt ID-document model — returns
/// structured key/value fields (name, DOB, document number, ...), not just raw text, unlike
/// <see cref="TesseractDocumentOcrService"/>. Selected via <see cref="OcrOptions.Provider"/>,
/// both behind the same <see cref="IDocumentOcrService"/> port so callers never bind to a vendor.
/// </summary>
public sealed class AzureDocumentIntelligenceOcrService : IDocumentOcrService
{
    private readonly OcrOptions _options;

    public AzureDocumentIntelligenceOcrService(IOptions<OcrOptions> options)
    {
        _options = options.Value;
    }

    public async Task<OcrResult> ExtractAsync(Stream document, CancellationToken cancellationToken)
    {
        var client = new DocumentIntelligenceClient(
            new Uri(_options.AzureEndpoint ?? throw new InvalidOperationException("Ocr:AzureEndpoint is not configured.")),
            new AzureKeyCredential(_options.AzureApiKey ?? throw new InvalidOperationException("Ocr:AzureApiKey is not configured.")));

        using var memoryStream = new MemoryStream();
        await document.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            _options.AzureModelId,
            BinaryData.FromStream(memoryStream),
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var analyzedDocument = result.Documents.Count > 0 ? result.Documents[0] : null;

        var fields = new Dictionary<string, string>();
        var confidence = 0d;

        if (analyzedDocument is not null)
        {
            foreach (var (name, field) in analyzedDocument.Fields)
            {
                if (field.Content is not null)
                {
                    fields[name] = field.Content;
                }
            }

            confidence = analyzedDocument.Confidence;
        }

        return new OcrResult(result.Content, fields, confidence);
    }
}
