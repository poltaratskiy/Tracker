using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Net;

namespace Tracker.Dotnet.Libs.Swagger;

public class GlobalErrorResponsesOperationProcessor : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        AddResponse(context, ((int)HttpStatusCode.BadRequest).ToString(), "Bad Request", typeof(ValidationProblemDetails));

        var authorizationRequired = DoesAuthorizationRequired(context);
        if (authorizationRequired)
        {
            AddResponse(context, ((int)HttpStatusCode.Unauthorized).ToString(), "Unauthorized");
            AddResponse(context, ((int)HttpStatusCode.Forbidden).ToString(), "Forbidden");
        }

        return true;
    }

    private void AddResponse(OperationProcessorContext ctx, string code, string description, Type? bodyType = null)
    {
        var operation = ctx.OperationDescription.Operation;

        if (!operation.Responses.TryGetValue(code, out var response))
        {
            response = new OpenApiResponse { Description = description };
            operation.Responses[code] = response;
        }

        if (bodyType is not null)
        {
            // Generate type scheme
            var generated = ctx.SchemaGenerator.Generate(bodyType, ctx.SchemaResolver);

            // Put or reuse the component-schmeme by the type name (in our case it is the ProblemDetails)
            var name = bodyType.Name;
            var components = ctx.Document.Components;
            if (!components.Schemas.TryGetValue(name, out var componentSchema))
            {
                components.Schemas[name] = generated;
                componentSchema = generated;
            }

            // Assign the component to the answer via Content
            response.Content["application/json"] = new OpenApiMediaType
            {
                Schema = new JsonSchema { Reference = componentSchema }
            };
        }
    }

    private bool DoesAuthorizationRequired(OperationProcessorContext context)
    {
        var method = context.MethodInfo;

        var methodHasAuthorize = method.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
        var typeHasAuthorize = method.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true;

        var methodAllowAnonymous = method.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();
        var typeAllowAnonymous = method.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() == true;

        // если указан [AllowAnonymous] на контроллере, он перекроет атрибуты на методе
        return (methodHasAuthorize || typeHasAuthorize) && !(methodAllowAnonymous || typeAllowAnonymous);
    }
}
