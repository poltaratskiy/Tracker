using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Tracker.Dotnet.Libs.Exceptions;

internal class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context); // calling next middleware
        }
        catch (ApiException ex)
        {
            _logger.LogInformation(ex, "Validation exception occured, message: {message}", ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = new ValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (WrongCredentialsException)
        {
            _logger.LogWarning("Wrong credentials were used");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var response = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Incorrect login or password",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occured");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Do not show details here
            var response = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
