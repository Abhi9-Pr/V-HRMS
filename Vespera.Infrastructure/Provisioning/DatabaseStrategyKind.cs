namespace Vespera.Infrastructure.Provisioning;

/// <summary>
/// Internal tag distinguishing candidates for the Production guard and Mode-based
/// registration filtering. Deliberately separate from the config-facing DatabaseMode enum —
/// SqliteFallback is never a value a developer can set via Mode, only something the Auto
/// chain reaches on its own.
/// </summary>
public enum DatabaseStrategyKind
{
    External,
    Local,
    Docker,
    SqliteFallback,
}
