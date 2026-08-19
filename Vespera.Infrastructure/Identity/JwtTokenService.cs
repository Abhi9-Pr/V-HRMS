using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Features.Auth;

namespace Vespera.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public string GenerateAccessToken(TokenClaims claims)
    {
        var subjectClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, claims.UserId.ToString()),
            new(ClaimTypes.Email, claims.Email),
            new(JwtClaimTypes.TenantId, claims.TenantId.Value.ToString()),
        };

        subjectClaims.AddRange(claims.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        subjectClaims.AddRange(claims.Permissions.Select(permission => new Claim(JwtClaimTypes.Permission, permission)));

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: subjectClaims,
            notBefore: now,
            expires: now.Add(AuthTokenLifetimes.AccessToken),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        try
        {
            return handler.ValidateToken(accessToken, parameters, out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
