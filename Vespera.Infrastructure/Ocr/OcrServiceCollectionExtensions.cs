using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Ocr;

public static class OcrServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaOcr(this IServiceCollection services)
    {
        services.AddSingleton<IDocumentOcrService, HeuristicDocumentOcrService>();
        return services;
    }
}
