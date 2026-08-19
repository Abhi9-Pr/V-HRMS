using Microsoft.Extensions.Options;

namespace Vespera.Infrastructure.Identity;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey) || options.SigningKey.Length < 32)
        {
            return ValidateOptionsResult.Fail(
                $"{JwtOptions.SectionName}:SigningKey must be set and at least 32 characters (HMAC-SHA256 requires a 256-bit key).");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail($"{JwtOptions.SectionName}:Issuer must be set.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail($"{JwtOptions.SectionName}:Audience must be set.");
        }

        return ValidateOptionsResult.Success;
    }
}
