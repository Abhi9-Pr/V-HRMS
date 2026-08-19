using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaStorage(this IServiceCollection services)
    {
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        return services;
    }
}
