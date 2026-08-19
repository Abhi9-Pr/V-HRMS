using System.Security.Claims;
using Vespera.Domain.Common;

namespace Vespera.Application.Abstractions.Identity;

public sealed record TokenClaims(Guid UserId, TenantId TenantId, string Email, IReadOnlyCollection<string> Roles);

public interface ITokenService
{
    public string GenerateAccessToken(TokenClaims claims);

    public string GenerateRefreshToken();

    public ClaimsPrincipal? ValidateAccessToken(string accessToken);
}
