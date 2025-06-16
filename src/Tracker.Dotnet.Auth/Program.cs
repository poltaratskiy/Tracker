using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Settings.Configuration;
using System.Reflection;
using Tracker.Dotnet.Auth.Configuration;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;
using Tracker.Dotnet.Auth.Services;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Libs.RefId;

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
        outputTemplate: "{Timestamp:O} [{Level:u3}] [{SourceContext}] {Scope:lj} {Message:lj}{NewLine}{Exception}")
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

    builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Set up password rules. I need to make possible password 123 just for testing this project but never use it in production environment!
    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 3;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 0;
    });

    builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

    builder.Services
        .AddScoped<ISignInManagerWrapper, SignInManagerWrapper>()
        .AddScoped<IRoleManagerWrapper, RoleManagerWrapper>()
        .AddScoped<IUserManagerWrapper, UserManagerWrapper>();

    builder.Services.AddSingleton<ITokenGeneratorService, TokenGeneratorService>();

    builder.Services
        .AddScoped<IRefreshTokenDbService, RefreshTokenDbService>()
        .AddScoped<IUnitOfWork, UnitOfWork>()
        .AddScoped<IUserService, UserService>()
        .AddScoped<AuthService>();

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

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
            "/var/log/myservices/auth-service.log",
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

    app.UseRefId();
    app.UseMyExceptionHandler();

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




