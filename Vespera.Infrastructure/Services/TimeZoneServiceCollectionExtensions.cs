using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Services;

public static class TimeZoneServiceCollectionExtensions
{
    /// <summary>Registers the NodaTime-backed <see cref="ITimeZoneConverter"/>. Stateless — no
    /// configuration section to bind, unlike <c>AddVesperaOcr</c>/<c>AddVesperaVirusScanning</c>
    /// which pick between vendor adapters.</summary>
    public static IServiceCollection AddVesperaTimeZoneConversion(this IServiceCollection services)
    {
        services.AddSingleton<ITimeZoneConverter, NodaTimeZoneConverter>();
        return services;
    }
}
