using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CurrencyApi.Api.Swagger;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SwaggerAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var allowsAnonymous = metadata.OfType<IAllowAnonymous>().Any();
        if (allowsAnonymous)
        {
            return;
        }

        var requiresAuthorization = metadata.OfType<IAuthorizeData>().Any();
        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                }
            ] = []
        });
    }
}
