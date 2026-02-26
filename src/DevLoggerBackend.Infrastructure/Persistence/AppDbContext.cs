using DevLoggerBackend.Application.Abstractions.Persistence;
using DevLoggerBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevLoggerBackend.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedUsers(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("9d2b489f-c8f9-4f36-98fd-4a1e0e9fdd11"),
                Name = "John Doe",
                Email = "john@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = Domain.Enums.UserRole.Developer,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = Guid.Parse("8ac15b3b-230c-43ad-8bc4-fcb1af7f1459"),
                Name = "Jane Smith",
                Email = "jane@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = Domain.Enums.UserRole.SeniorDeveloper,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = Guid.Parse("6cf97b16-a4b9-448f-8887-f6c8a21a58ec"),
                Name = "Bob Lee",
                Email = "bob@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = Domain.Enums.UserRole.TeamLead,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
    }
}
