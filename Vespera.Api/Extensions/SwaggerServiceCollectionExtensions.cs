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
            //
            // The bare name is NOT unique across the whole API surface — many controllers already
            // share a "List"/"Create"/"Delete" action, and NSwag's client generator silently
            // appends a numeric suffix (list2, create7, ...) to whichever controller's operation
            // it encounters second, which is ApiDescription ordering-dependent and can shift on
            // any unrelated controller addition anywhere in the assembly. (A "{Controller}_{Action}"
            // operationId would make every id unique, but nswag.json's
            // "MultipleClientsFromFirstTagAndOperationId" mode uses the operationId verbatim as
            // the per-client method name rather than stripping the tag prefix back off, so that
            // trade gets a globally-unique id at the cost of every generated method being renamed
            // to "leaveClient.leave_SubmitLeaveRequest(...)" — worse than the collision it fixes.)
            // The real fix is to keep each new controller's own action names unique against the
            // rest of the surface as they're added — see e.g. LeaveController's
            // "SubmitLeaveRequest"/"ApproveLeaveRequest" instead of "Submit"/"Approve".
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
