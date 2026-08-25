using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;
using Vespera.Infrastructure.Services;

namespace Vespera.Infrastructure.UnitTests.Services;

/// <summary>
/// <see cref="TesseractDocumentOcrService"/> wraps the native Tesseract engine and
/// <see cref="AzureDocumentIntelligenceOcrService"/> wraps a real Azure SDK client — neither is
/// reasonably fakeable without an over-engineered indirection layer this codebase doesn't
/// otherwise use, so what's actually tested here is the one piece of real logic:
/// <see cref="OcrServiceCollectionExtensions.AddVesperaOcr"/>'s provider selection.
/// </summary>
public class OcrServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Tesseract", typeof(TesseractDocumentOcrService))]
    [InlineData("AzureDocumentIntelligence", typeof(AzureDocumentIntelligenceOcrService))]
    [InlineData("SomeUnknownProvider", typeof(TesseractDocumentOcrService))]
    public void AddVesperaOcr_Should_Register_The_Adapter_Matching_The_Configured_Provider(string provider, Type expectedType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"{OcrOptions.SectionName}:Provider"] = provider })
            .Build();

        var services = new ServiceCollection();
        services.AddVesperaOcr(configuration);
        var provider2 = services.BuildServiceProvider();

        var resolved = provider2.GetRequiredService<IDocumentOcrService>();

        resolved.Should().BeOfType(expectedType);
    }
}
