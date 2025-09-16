using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Tracker.Dotnet.Libs.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddTrackerSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(options =>
        {
            options.Title = "FusionAuth-protected API";
            options.Version = "v1";

            // Adding security scheme
            options.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Insert your access token from FusionAuth. Format: only token without Bearer and etc.",
                In = OpenApiSecurityApiKeyLocation.Header,
                Name = "Authorization"
            });

            // Apply this scheme to all methods
            options.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            options.OperationProcessors.Add(new GlobalErrorResponsesOperationProcessor());
        });

        return services;
    }

    internal static IApplicationBuilder UseTrackerSwagger(this IApplicationBuilder app, string serviceName)
    {
        app.UseOpenApi();
        app.UseSwaggerUi(options =>
        {
            options.DocumentTitle = serviceName;
            options.OperationsSorter = "alpha";
            options.TagsSorter = "alpha";

            options.AdditionalSettings["persistAuthorization"] = true;
        });

        return app;
    }
}
