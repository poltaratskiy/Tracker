using Serilog;
using System.Reflection;
using Tracker.Dotnet.Libs.Authorization;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Libs.Logging;
using Tracker.Dotnet.Libs.RefId;
using Tracker.Dotnet.Libs.Swagger;
using Tracker.Dotnet.Users.External;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;
var appName = Assembly.GetCallingAssembly().GetName().Name;

services.InitializeGlobalLogger(builder.Environment);

try
{
    Log.Information($"Starting {appName} application...");

    services.AddControllers();

    builder.Host.AddSerilog(configuration);

    services.AddExternalServices(configuration);
    services.AddTrackerSwagger();
    services.AddJwtTrackerAuthentication(configuration);

    // Add authorization
    builder.Services.AddAuthorization();

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

    var app = builder.Build();
    Log.Information($"Configuring services at {appName} has been finished");

    app.UseRefId();
    app.UseMyExceptionHandler();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsProduction())
    {
        app.UseTrackerSwagger();
    }

    // Order is important, first authentication, then authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, $"Application {appName} terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

