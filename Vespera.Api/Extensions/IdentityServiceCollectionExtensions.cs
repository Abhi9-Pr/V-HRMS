using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Vespera.Api.Authorization;
using Vespera.Infrastructure.Identity;

namespace Vespera.Api.Extensions;

public static class IdentityServiceCollectionExtensions
{
    public static WebApplicationBuilder AddVesperaIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddVesperaIdentityInfrastructure(builder.Configuration);

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                // SignalR's WebSocket handshake can't set an Authorization header, so the hub
                // accepts the token via query string instead — restricted to the hub path only.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, NotCreatorAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, NotDryRunExecutorAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, SubordinateOrSelfAuthorizationHandler>();
        builder.Services.AddAuthorization();

        return builder;
    }
}
