namespace Vespera.Infrastructure.Services;

public sealed class OcrOptions
{
    public const string SectionName = "Vespera:Ocr";

    /// <summary>"Tesseract" (local) or "AzureDocumentIntelligence" (cloud).</summary>
    public string Provider { get; set; } = "Tesseract";

    /// <summary>Directory containing the Tesseract <c>.traineddata</c> language files.</summary>
    public string TesseractDataPath { get; set; } = "tessdata";

    public string TesseractLanguage { get; set; } = "eng";

    public string? AzureEndpoint { get; set; }

    public string? AzureApiKey { get; set; }

    /// <summary>Azure Document Intelligence model id used for ID-document extraction.</summary>
    public string AzureModelId { get; set; } = "prebuilt-idDocument";
}
