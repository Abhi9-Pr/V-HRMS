using Microsoft.Extensions.DependencyInjection;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Attendance;

public static class BiometricServiceCollectionExtensions
{
    /// <summary>
    /// Registers the one biometric vendor adapter this codebase implements today
    /// (<see cref="ZkTecoBiometricDeviceAdapter"/>) behind <see cref="IBiometricDeviceAdapter"/>.
    /// With only one vendor, there's no provider-switch to make yet (unlike
    /// <c>AddVesperaOcr</c>/<c>AddVesperaVirusScanning</c>'s two-adapter selection) — when a second
    /// vendor adapter is added, extend this method with the same
    /// <c>configuration["Provider"]</c>-driven switch those use, per
    /// docs/adding-a-biometric-vendor.md.
    /// </summary>
    public static IServiceCollection AddVesperaBiometricDevices(this IServiceCollection services)
    {
        services.AddHttpClient<IBiometricDeviceAdapter, ZkTecoBiometricDeviceAdapter>();
        return services;
    }
}
