using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DevLoggerBackend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TasksWorked = table.Column<string>(type: "text", nullable: false),
                    ProblemsFaced = table.Column<string>(type: "text", nullable: false),
                    Solutions = table.Column<string>(type: "text", nullable: false),
                    Learnings = table.Column<string>(type: "text", nullable: false),
                    Tips = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    GitLink = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAtUtc", "Email", "Name", "PasswordHash", "Role", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("6cf97b16-a4b9-448f-8887-f6c8a21a58ec"), new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207), "bob@example.com", "Bob Lee", "$2a$11$aHO3NhtzdhgfgbONK3xn7uNQj0B3MLSme.lggL8AxWn0lH2c1UdPO", 3, new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207) },
                    { new Guid("8ac15b3b-230c-43ad-8bc4-fcb1af7f1459"), new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207), "jane@example.com", "Jane Smith", "$2a$11$SuL1XY1j2PPACdaQClHZ2ejJjRIHvew5ALae.U4dLGuoO04fOOI66", 2, new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207) },
                    { new Guid("9d2b489f-c8f9-4f36-98fd-4a1e0e9fdd11"), new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207), "john@example.com", "John Doe", "$2a$11$gHj0CTIF6KI2Rvl4hqzw1uBaTw9YAkfQourqTmM9kerpKOFI1c.Fe", 1, new DateTime(2026, 2, 27, 17, 5, 34, 840, DateTimeKind.Utc).AddTicks(5207) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_LogDate",
                table: "DailyLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_UserId",
                table: "DailyLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyLogs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
