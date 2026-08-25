using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection(LocalFileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<LocalFileStorage>(sp => (LocalFileStorage)sp.GetRequiredService<IFileStorage>());
        return services;
    }
}
