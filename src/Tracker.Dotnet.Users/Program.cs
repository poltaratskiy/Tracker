using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using System.Reflection;
using System.Security.Claims;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Libs.RefId;
using Tracker.Dotnet.Users.External;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

var appName = Assembly.GetCallingAssembly().GetName().Name;
var env = builder.Environment;
var appEnvironment = env.IsDevelopment() ? "Development" : "Production";

#region logger
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
#endregion logger

try
{
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    Log.Information($"Starting {assemblyName} application...");

    services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(o =>
    {
        o.SwaggerDoc("v1", new OpenApiInfo { Title = "Keycloak-protected API", Version = "v1" });
        o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Insert your access token from FusionAuth. Format: only token without Bearer and etc.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    #region useLogger
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
    #endregion uselogger

    services.AddHttpContextAccessor();

    var jwt = builder.Configuration.GetSection("Jwt");
    var authority = jwt["Authority"] ?? throw new InvalidOperationException("Jwt:Authority not set");
    var audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience not set");
    var requireHttps = bool.TryParse(jwt["RequireHttpsMetadata"], out var r) ? r : true;
    var validateIssuer = bool.TryParse(jwt["ValidateIssuer"], out var v) ? v : true;

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = requireHttps;

            options.TokenValidationParameters.NameClaimType = "username";  // username is in this claim
            options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role; // roles are in this claim
            options.TokenValidationParameters.ValidIssuer = authority;
            options.TokenValidationParameters.ValidAudience = audience;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.RequireSignedTokens = false; // dev
            options.TokenValidationParameters.ValidateIssuerSigningKey = false; // dev

            options.TokenValidationParameters.SignatureValidator = (token, parameters) =>
            {
                // validate just existing of the signature because here is used self signed certificate
                return new JsonWebToken(token);
            };

            options.IncludeErrorDetails = true;
        });

    // Add authorization
    builder.Services.AddAuthorization();

    services.AddExternalServices(configuration);
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

    var app = builder.Build();
    Log.Information($"Configuring services at {assemblyName} has been finished");

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
    Log.Fatal(ex, $"Application {appName} terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

