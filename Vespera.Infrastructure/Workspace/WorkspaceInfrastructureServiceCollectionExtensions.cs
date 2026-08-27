using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Workspace;

public static class WorkspaceInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaWorkspace(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IDashboardWidgetCache, MemoryDashboardWidgetCache>();
        return services;
    }
}
