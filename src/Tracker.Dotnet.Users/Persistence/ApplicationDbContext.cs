using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Users.Models.Entities;

namespace Tracker.Dotnet.Users.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Login)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
