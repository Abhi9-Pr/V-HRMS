using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Services;

public static class OcrServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaOcr(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OcrOptions>()
            .Bind(configuration.GetSection(OcrOptions.SectionName));

        var provider = configuration.GetSection(OcrOptions.SectionName)["Provider"] ?? "Tesseract";

        if (string.Equals(provider, "AzureDocumentIntelligence", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IDocumentOcrService, AzureDocumentIntelligenceOcrService>();
        }
        else
        {
            services.AddScoped<IDocumentOcrService, TesseractDocumentOcrService>();
        }

        return services;
    }
}
