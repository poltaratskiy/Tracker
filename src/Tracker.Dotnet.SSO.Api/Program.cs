using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using System.Reflection;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Libs.RefId;
using Tracker.Dotnet.SSO.Api.Domain;
using Tracker.Dotnet.SSO.Api.Infrastructure;
using Tracker.Dotnet.SSO.Api.Persistence;
using Tracker.Dotnet.SSO.Persistence;

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

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // Add services to the container.
    //builder.Services.AddHttpContextAccessor();

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("ApplicationDbContext")));

    services.AddIdentityCore<User>(o =>
    {
        o.Password.RequiredLength = 3;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireDigit = false;
        o.Password.RequireUppercase = false;
    })   
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();


    services.Configure<IdentityOptions>(o =>
    {
        // Synchronize claim types for JWT/OpenIddict
        o.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject; // "sub"
        o.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;    // "name"
        o.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;    // "role"
    });

    // adding cookies not to login every time
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    })
        .AddCookie(IdentityConstants.ApplicationScheme, o =>
        {
            o.Cookie.Name = ".SSO.Auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.None; // Just for docker because using http
            o.Cookie.SameSite = SameSiteMode.Lax;// SPA-friendly
            o.SlidingExpiration = true;
            o.ExpireTimeSpan = TimeSpan.FromDays(14);
            // API must not redirect
            o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
            o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
        });

    services.AddAuthorization();

    services.AddOpenIddict()
        .AddCore(o => o.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
        .AddServer(o =>
        {
            o.SetAuthorizationEndpointUris("/connect/authorize")
             .SetTokenEndpointUris("/connect/token")
             .SetRevocationEndpointUris("/connect/revoke")
             .SetUserInfoEndpointUris("/connect/userinfo")
             .SetEndSessionEndpointUris("/connect/endsession");

            // flows and grants
            o.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange(); // code + PKCE
            o.AllowRefreshTokenFlow();

            // scopes
            o.RegisterScopes("openid", "profile", "email", "offline_access", "api.read", "api.write");

            // JWT - for compatibility with Microsoft libraries
            o.DisableAccessTokenEncryption();
            // for pet project can use ephemeral keys; only certificates in production!
            o.AddEphemeralSigningKey().AddEphemeralEncryptionKey();

            // Lifetime
            o.SetAccessTokenLifetime(TimeSpan.FromMinutes(10));
            o.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

            // ASP.NET Core integration (forwards)
            o.UseAspNetCore()
                .EnableAuthorizationEndpointPassthrough()
                .EnableTokenEndpointPassthrough()
                .EnableUserInfoEndpointPassthrough()
                .EnableEndSessionEndpointPassthrough()
                .DisableTransportSecurityRequirement(); // dev/http;

            o.AddEventHandler<OpenIddictServerEvents.ProcessSignInContext>(b =>
                b.UseScopedHandler<IncludeRolesInTokensHandler>());
        })
        ;//.AddValidation(o => { o.UseLocalServer(); o.UseAspNetCore(); });

 /*   services.AddAntiforgery(o =>
    {
        o.Cookie.Name = ".SSO.CSRF";
        o.Cookie.SameSite = SameSiteMode.Lax;        // important for cross-site XHR
        o.Cookie.SecurePolicy = CookieSecurePolicy.None; // must be always but this project does not use SSL
        o.HeaderName = "X-CSRF";
        o.Cookie.HttpOnly = false;          // to UI could read token from answer and put into header
    });
 */
    // to keep keys and cookie didn't disappear when restarted
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
        .SetApplicationName("tracker-sso");

    services.AddCors(o => o.AddPolicy("sso-web", p =>
    {
         p.WithOrigins("http://localhost:5733")
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials();
    }));

    services.AddHostedService<OpenIddictSeeder>();

    // Add services to the container.

    builder.Services.AddControllers();

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

    // turn on after sso ui will be ready
    //app.UseCors("sso-web");

    app.UseRefId();
    app.UseMyExceptionHandler();

    app.UseAuthentication();
    app.Use(async (ctx, next) =>
    {
        // Пример: защищаем state‑changing UI‑API анти-CSRF
        if (HttpMethods.IsPost(ctx.Request.Method) && ctx.Request.Path.StartsWithSegments("/api/session"))
        {
            var anti = ctx.RequestServices.GetRequiredService<IAntiforgery>();
            await anti.ValidateRequestAsync(ctx);
        }
        await next();
    });

    app.UseAuthentication();
    app.UseAuthorization();

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
