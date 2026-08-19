using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Vespera.Api.Http;

/// <summary>
/// Mobile affordance #2 (see docs/api-mobile-contract.md): ETag/If-None-Match support for
/// reference-data GETs. Plumbing only this phase — no endpoint calls this yet; a later phase's
/// reference-data queries (departments, designations, leave types, ...) call
/// <see cref="TryShortCircuit"/> once they have a stable version stamp (e.g. RowVersion or
/// ModifiedAt) to build the tag from.
/// </summary>
public static class ETagNegotiation
{
    /// <summary>If the request's If-None-Match matches <paramref name="tag"/>, writes 304 and
    /// returns true (the caller should stop and return nothing else). Otherwise sets the
    /// response's ETag header and returns false.</summary>
    public static bool TryShortCircuit(HttpContext context, string tag)
    {
        var quoted = $"\"{tag}\"";

        if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch.Contains(quoted))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        context.Response.Headers[HeaderNames.ETag] = quoted;
        return false;
    }
}
