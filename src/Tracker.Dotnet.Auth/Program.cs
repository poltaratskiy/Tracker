﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using System.Reflection;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    Log.Information($"Starting {assemblyName} application...");

    // Add services to the container.
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("ApplicationDbContext")));

    builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    services.AddSerilog(loggerConfiguration =>
    {
        var appName = Assembly.GetCallingAssembly().GetName().Name;
        var env = builder.Environment;
        var appEnvironment = env.IsDevelopment() ? "Development" : "Production";

        loggerConfiguration
            .MinimumLevel.Debug()
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", env.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName)
            .Enrich.WithProperty("ApplicationEnvironment", appEnvironment)
            .Enrich.WithExceptionDetails()
            .ReadFrom.Configuration(configuration, new ConfigurationReaderOptions
            {
                SectionName = "Logging",
            });

        //elastic or something similar

        if (env.IsDevelopment())
        {
            loggerConfiguration.WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: "{Timestamp:O} [{Level:u3}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}");
        }
        else
        {
            loggerConfiguration.WriteTo.Console(
                formatter: new Serilog.Formatting.Compact.RenderedCompactJsonFormatter(),
                restrictedToMinimumLevel: LogEventLevel.Information);
        }
    });

    var app = builder.Build();
    Log.Information($"Configuring services at {assemblyName} has been finished");

    using (var scope = app.Services.CreateScope())
    {
        var scopeProvider = scope.ServiceProvider;
        var logger = scopeProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting Postgres DB migration...");
            var db = scopeProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();

            logger.LogInformation("DB migration successfully finished. Start seeding test data...");
            await SeedData.AddRoles(scopeProvider);
            logger.LogInformation("Seeding test data successfully finished.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while migrating db, application won't be started");
            throw;
        }
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application Tracker.Dotnet.Auth terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}



/*builder.Services.AddAuthentication(o => для сервиса где нужна авторизация
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).
    .AddJwtBearer(...);
builder.Services.AddAuthorization(); // потом посмотреть подробнее
*/






/* app.UseHttpsRedirection();
 * Disabled https only for this pet project to avoid issues with certificate installations.
 * Authorization service which provides sensitive information such as passwords and tokens,
 * MUST use https on production environment
 */

/*app.UseAuthentication(); понадобятся в обычных сервисах
app.UseAuthorization();*/




