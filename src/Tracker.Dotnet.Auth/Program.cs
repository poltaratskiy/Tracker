using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

#region loggerconfig
var loggerConfig = new Action<LoggerConfiguration>((loggerConfiguration) =>
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
#endregion loggerconfig

// Add services to the container.
services.AddSerilog(loggerConfig);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("ApplicationDbContext")));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

/*builder.Services.AddAuthentication(o => для сервиса где нужна авторизация
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).
    .AddJwtBearer(...);
builder.Services.AddAuthorization(); // потом посмотреть подробнее
*/

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var scopeProvider = scope.ServiceProvider;
    var logger = scopeProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Starting Postgres DB migration...");
        var db = scopeProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        logger.LogInformation("DB migration successfully finished");
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

/* app.UseHttpsRedirection();
 * Disabled https only for this pet project to avoid issues with certificate installations.
 * Authorization service which provides sensitive information such as passwords and tokens,
 * MUST use https on production environment
 */

/*app.UseAuthentication(); понадобятся в обычных сервисах
app.UseAuthorization();*/

app.MapControllers();

app.Run();


