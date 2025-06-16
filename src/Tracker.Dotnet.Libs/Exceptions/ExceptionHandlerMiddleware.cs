using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tracker.Dotnet.Libs.ApiResponse;

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
            _logger.LogInformation(ex, "Validation exception occured, message: {message}, details: {details}", ex.Message, string.Join(" ;", ex.Details));

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            var response = ApiResponse<string>.Fail(ex.Message, ex.Details);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (WrongCredentialsException)
        {
            _logger.LogWarning("Wrong credentials were used");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            var response = ApiResponse<string>.Fail("Incorrect login or password", null);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occured");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // Do not show details here
            var response = ApiResponse<string>.Fail("Internal Server Error, please repeat operation or contact an administrator", null);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
