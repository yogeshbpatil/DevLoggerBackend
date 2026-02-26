using DevLoggerBackend.Api.Middleware;
using DevLoggerBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevLoggerBackend.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseCors("FrontendPolicy");

        // TODO: Enable JWT middleware in next auth phase.
        // app.UseAuthentication();
        // app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("StartupMigrations");
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "28P01")
        {
            logger.LogError(ex, "Database authentication failed for configured PostgreSQL connection.");

            if (!app.Environment.IsDevelopment())
            {
                throw;
            }

            logger.LogWarning(
                "Continuing startup in Development without applying migrations. " +
                "Update ConnectionStrings:DefaultConnection with valid PostgreSQL credentials.");
        }
    }
}
