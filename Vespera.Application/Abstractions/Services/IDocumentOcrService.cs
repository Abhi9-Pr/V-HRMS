namespace Vespera.Application.Abstractions.Services;

public sealed record OcrResult(string ExtractedText, IReadOnlyDictionary<string, string> Fields, double Confidence);

public interface IDocumentOcrService
{
    public Task<OcrResult> ExtractAsync(Stream document, CancellationToken cancellationToken);
}
