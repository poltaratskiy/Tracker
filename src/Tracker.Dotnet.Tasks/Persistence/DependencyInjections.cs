using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Tasks.Persistence.Seeding;

namespace Tracker.Dotnet.Tasks.Persistence;

internal static class DependencyInjections
{
    internal static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddDbContext<ApplicationDbContext>(cfg =>
            cfg.UseNpgsql(configuration.GetConnectionString("ApplicationDbContext"))
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                var seeder = new UsersSeeder((ApplicationDbContext)context);
                await seeder.SeedAsync(cancellationToken);
            }));

        return services;
    }

    internal async static Task<IApplicationBuilder> MigrateDbAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        return app;
    }
}
