using Microsoft.AspNetCore.Http;

namespace Vespera.Api.Http;

/// <summary>
/// Mobile affordance #3 (see docs/api-mobile-contract.md): compact-DTO content negotiation.
/// Reads the <c>X-Response-Shape: compact</c> header once per request; a mapping profile can
/// later consult <see cref="IsCompact"/> to project onto a <c>*SummaryDto</c> instead of the full
/// DTO (see CONTRIBUTING-slices.md's Dto/SummaryDto convention — this is the negotiation signal,
/// not a new mapping mechanism). Plumbing only this phase — no query consults it yet.
/// </summary>
public interface ICompactResponseContext
{
    public bool IsCompact { get; }
}

public sealed class CompactResponseContext : ICompactResponseContext
{
    private const string HeaderName = "X-Response-Shape";
    private const string CompactValue = "compact";

    public CompactResponseContext(IHttpContextAccessor httpContextAccessor)
    {
        var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
        IsCompact = string.Equals(header, CompactValue, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsCompact { get; }
}
