namespace Vespera.Application.Abstractions.Services;

public sealed record PdfGenerationRequest(string TemplateKey, IReadOnlyDictionary<string, object?> Data);

public interface IPdfGenerator
{
    public Task<byte[]> GenerateAsync(PdfGenerationRequest request, CancellationToken cancellationToken);
}
