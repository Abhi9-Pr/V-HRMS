namespace Vespera.Api.Http;

/// <summary>Optional IP/network allowlist for the web punch endpoint — off by default, since the
/// requirement calls it out as optional. When enabled, a punch from an address outside every
/// listed CIDR block is rejected before the command even runs (a network-layer check belongs at
/// the Api edge, not in Application).</summary>
public sealed class WebPunchOptions
{
    public const string SectionName = "Vespera:WebPunch";

    public bool AllowlistEnabled { get; set; }

    public string[] Cidrs { get; set; } = [];
}
