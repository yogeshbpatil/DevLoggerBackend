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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

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
            var repairedLegacyHistory = await RepairLegacyMigrationHistoryAsync(dbContext);
            if (repairedLegacyHistory)
            {
                logger.LogWarning(
                    "Existing Developer Journal tables were found without the initial EF migration history record. " +
                    "The initial migration was marked as applied so pending migrations can run without changing existing data.");
            }

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

    private static async Task<bool> RepairLegacyMigrationHistoryAsync(AppDbContext dbContext)
    {
        await dbContext.Database.OpenConnectionAsync();

        try
        {
            await using var schemaCommand = dbContext.Database.GetDbConnection().CreateCommand();
            schemaCommand.CommandText = """
                SELECT to_regclass('"Users"') IS NOT NULL
                   AND to_regclass('"DailyLogs"') IS NOT NULL
                   AND to_regclass('"Notes"') IS NOT NULL;
                """;

            var hasExistingJournalSchema = (bool)(await schemaCommand.ExecuteScalarAsync() ?? false);
            if (!hasExistingJournalSchema)
            {
                return false;
            }

            await using var historyCommand = dbContext.Database.GetDbConnection().CreateCommand();
            historyCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );

                ALTER TABLE "Notes"
                ALTER COLUMN "Content" TYPE text;
                """;

            await historyCommand.ExecuteNonQueryAsync();

            await using var baselineCommand = dbContext.Database.GetDbConnection().CreateCommand();
            baselineCommand.CommandText = """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('20260725125919_InitialCreate', '10.0.0')
                ON CONFLICT ("MigrationId") DO NOTHING;
                """;

            return await baselineCommand.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
