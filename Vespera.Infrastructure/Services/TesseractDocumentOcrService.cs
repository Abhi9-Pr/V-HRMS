using Microsoft.Extensions.Options;
using Tesseract;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Services;

/// <summary>
/// Local OCR via the Tesseract engine — no network call, but requires the native Tesseract
/// binaries and a <c>.traineddata</c> language file on disk at <see cref="OcrOptions.TesseractDataPath"/>.
/// Only produces raw text; structured field extraction (name, DOB, id number, ...) is
/// <see cref="AzureDocumentIntelligenceOcrService"/>'s job — selected between via
/// <see cref="OcrOptions.Provider"/>, both behind the same <see cref="IDocumentOcrService"/> port.
/// </summary>
public sealed class TesseractDocumentOcrService : IDocumentOcrService
{
    private readonly OcrOptions _options;

    public TesseractDocumentOcrService(IOptions<OcrOptions> options)
    {
        _options = options.Value;
    }

    public Task<OcrResult> ExtractAsync(Stream document, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        document.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        using var engine = new TesseractEngine(_options.TesseractDataPath, _options.TesseractLanguage, EngineMode.Default);
        using var image = Pix.LoadFromMemory(bytes);
        using var page = engine.Process(image);

        var text = page.GetText();
        var confidence = page.GetMeanConfidence();

        return Task.FromResult(new OcrResult(text, new Dictionary<string, string>(), confidence));
    }
}
