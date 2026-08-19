using System.Runtime.CompilerServices;
using Mapster;

namespace Vespera.Application.Mapping;

/// <summary>
/// Registers every feature's IRegister the moment this assembly loads, so Adapt&lt;T&gt;()
/// works everywhere — including unit tests — without depending on AddApplication having run.
/// </summary>
internal static class MapsterModuleInitializer
{
    // CA2255 guards published libraries against surprising their consumers. Vespera.Application
    // is only ever consumed within this solution, and every consumer (DI-based app, unit
    // tests) needs this scan to have run — a module initializer is the only way that doesn't
    // depend on every consumer remembering to call it themselves.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        TypeAdapterConfig.GlobalSettings.Scan(AssemblyReference.Assembly);
    }
}
