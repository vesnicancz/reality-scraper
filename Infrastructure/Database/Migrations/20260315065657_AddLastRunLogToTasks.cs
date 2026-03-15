using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealityScraper.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLastRunLogToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRunLog",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastRunSucceeded",
                table: "Tasks",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRunLog",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LastRunSucceeded",
                table: "Tasks");
        }
    }
}
