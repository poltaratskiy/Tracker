using Microsoft.AspNetCore.Builder;

namespace Tracker.Dotnet.Libs.Exceptions;

public static class ExceptionExtensions
{
    public static IApplicationBuilder UseMyExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        return app;
    }
}
