using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;

namespace Vespera.Api.Extensions;

public static class SwaggerServiceCollectionExtensions
{
    public static WebApplicationBuilder AddVesperaSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Vespera HRMS API", Version = "v1" });

            // Without an explicit operationId, NSwag falls back to "{route-segment}{HTTPMETHOD}"
            // (e.g. "departmentsGET2") for the generated TypeScript client — the action's own
            // method name is already a good, unique-per-controller identifier.
            options.CustomOperationIds(description =>
                description.ActionDescriptor is ControllerActionDescriptor controllerAction ? controllerAction.MethodInfo.Name : null);

            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a JWT access token issued by /api/v1/auth/login.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                    Array.Empty<string>()
                },
            });
        });

        return builder;
    }
}
