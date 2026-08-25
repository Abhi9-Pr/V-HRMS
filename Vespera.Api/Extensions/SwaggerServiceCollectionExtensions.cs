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

            // Controller-qualified, not just the bare method name: NSwag's
            // MultipleClientsFromFirstTagAndOperationId mode groups generated client *classes* by
            // tag (controller), but still requires operationId to be unique across the *whole*
            // document, not just within one controller — two controllers both having a "Create"
            // action (a routine occurrence as more CRUD-shaped features are added) collide and get
            // silently renumbered ("create" / "create2" / ...) in whatever order Swashbuckle
            // enumerates actions, which shifts unpredictably every time a new colliding action is
            // added anywhere in the API and has already broken existing generated-client callers
            // (Vespera.Client/src/app/features/departments/data/departments.facade.ts) once.
            // Qualifying by controller name guarantees global uniqueness deterministically.
            options.CustomOperationIds(description =>
                description.ActionDescriptor is ControllerActionDescriptor controllerAction
                    ? $"{controllerAction.ControllerName}_{controllerAction.MethodInfo.Name}"
                    : null);

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
