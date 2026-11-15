using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevLoggerBackend.Infrastructure.Migrations;

/// <inheritdoc />
[Migration("20261115150000_AllowUnlimitedNoteContent")]
public partial class AllowUnlimitedNoteContent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Content",
            table: "Notes",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100000)",
            oldMaxLength: 100000);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Content",
            table: "Notes",
            type: "character varying(100000)",
            maxLength: 100000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");
    }
}
