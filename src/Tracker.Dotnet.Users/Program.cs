using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Libs.RefId;
using Tracker.Dotnet.Users.Persistence;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

Serilog.Debugging.SelfLog.Enable(Console.Error);

var appName = Assembly.GetCallingAssembly().GetName().Name;
var env = builder.Environment;
var appEnvironment = env.IsDevelopment() ? "Development" : "Production";

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
    "/var/log/myservices/auth-service.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7);

// Creates global logger for logging DI initialization
Log.Logger = loggerConfiguration
    .CreateLogger();

try
{
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    Log.Information($"Starting {assemblyName} application...");

    // Add services to the container.
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("ApplicationDbContext")));

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Input JWT-token only (without Bearer)"
        });

        // Point out that all endpoints have this scheme
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                 new OpenApiSecurityScheme
                 {
                     Reference = new()
                     {
                         Type = ReferenceType.SecurityScheme,
                         Id = "Bearer"
                     }
                 },
                 Array.Empty<string>()
             }
         });
    });

    builder.Host.UseSerilog((context, services, config) =>
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
            "/var/log/myservices/users-service.log",
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

    var jwtKey = builder.Configuration["Jwt:SymmetricKey"] ?? throw new Exception("Jwt:SymmetricKey is missing");

    // Adding JWT Authentication. Don't check issuer and audience for this test case. We don't use database because it's in another service.
    // We trust the token if signature is valid and it means that the user exists and claims are correct.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name
            };
        });

    // Add authorization
    builder.Services.AddAuthorization();

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
            await SeedData.Seed(scopeProvider);
            logger.LogInformation("Seeding test data successfully finished.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while migrating db, application won't be started");
            throw;
        }
    }

    app.UseRefId();
    app.UseMyExceptionHandler();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Order is important, first authentication, then authorization
    app.UseAuthentication();
    app.UseAuthorization();

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

