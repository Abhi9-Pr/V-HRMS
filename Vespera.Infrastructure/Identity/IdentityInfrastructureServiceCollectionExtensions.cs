using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Identity;

public static class IdentityInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddVesperaIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IOtpService, OtpService>();

        services.AddScoped<IUserStore<ApplicationUser>, VesperaUserStore>();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // Argon2/PBKDF2 "per current guidance": ASP.NET Core's PasswordHasher<T> is
                // PBKDF2-HMACSHA256; the default iteration count is raised to OWASP's 2023
                // recommendation. Every other Identity concern this app doesn't use (lockout,
                // sign-in cookies, phone, external logins) stays at its default/unused — only
                // password + email + security-stamp options are meaningful with this store.
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = true;
            })
            .AddUserStore<VesperaUserStore>()
            .AddDefaultTokenProviders();

        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);

        services.AddScoped<IUserCredentialStore, EfUserCredentialStore>();
        services.AddScoped<IAccessRevocationService, AccessRevocationService>();

        return services;
    }
}
