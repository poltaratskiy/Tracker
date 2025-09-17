using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using System.Reflection;
using Tracker.Dotnet.Libs.RefId;

namespace Tracker.Dotnet.Libs.Logging;

public static class DependencyExtensions
{
    public static IServiceCollection InitializeGlobalLogger(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var appName = Assembly.GetCallingAssembly().GetName().Name;
        var appEnvironment = environment.IsDevelopment() ? "Development" : "Production";

        var loggerConfiguration = new LoggerConfiguration()
             .MinimumLevel.Debug()
             .Enrich.FromLogContext()
             .Enrich.WithProperty("Application", appName)
             .Enrich.WithProperty("ApplicationEnvironment", appEnvironment)
             .Enrich.WithExceptionDetails()
             .WriteTo.Console(
                 restrictedToMinimumLevel: LogEventLevel.Debug,
                 outputTemplate: "{Timestamp:O} [{Application}] [{Level:u3}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}")
             .WriteTo.File(
                 $"/var/log/myservices/{appName}.log",
                 rollingInterval: RollingInterval.Day,
                 retainedFileCountLimit: 7);

        // Creates global logger for logging DI initialization
        Log.Logger = loggerConfiguration
            .CreateLogger();

        return services;
    }

    public static IHostBuilder AddSerilog(this IHostBuilder builder, IConfiguration configuration)
    {
        builder.UseSerilog((context, services, config) =>
        {
            var appName = Assembly.GetEntryAssembly()!.GetName().Name;
            var env = context.HostingEnvironment;
            var appEnvironment = env.IsDevelopment() ? "Development" : "Production";

            config
                .MinimumLevel.Information()
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", appName)
                .Enrich.WithProperty("ApplicationEnvironment", appEnvironment)
                .Enrich.WithExceptionDetails()
                .WithRefId(services)
                .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
                {
                    SectionName = "Logging",
                });

            //elastic or something similar
            config.WriteTo.File(
                $"/var/log/myservices/{appName}.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:O} [{Application}] [{Level:u3}] [{RefId}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}");

            if (env.IsDevelopment())
            {
                config.WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "{Timestamp:O} [{Application}] [{Level:u3}] [{RefId}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                config.WriteTo.Console(
                    outputTemplate: "{Timestamp:O} [{Application}] [{Level:u3}] [{RefId}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }
        });

        return builder;
    }
}
