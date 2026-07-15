using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealityScraper.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRemovedListingsReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listing_ScraperTaskId",
                table: "Listing");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Tasks",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSuccessfulReportAt",
                table: "Tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovedAt",
                table: "Listing",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportTaskRecipient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTaskRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTaskRecipient_Tasks_ReportTaskId",
                        column: x => x.ReportTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportTaskSource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTaskSource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportTaskSource_Tasks_ReportTaskId",
                        column: x => x.ReportTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportTaskSource_Tasks_ScraperTaskId",
                        column: x => x.ScraperTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_ScraperTaskId_RemovedAt",
                table: "Listing",
                columns: new[] { "ScraperTaskId", "RemovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTaskRecipient_ReportTaskId",
                table: "ReportTaskRecipient",
                column: "ReportTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTaskSource_ReportTaskId_ScraperTaskId",
                table: "ReportTaskSource",
                columns: new[] { "ReportTaskId", "ScraperTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportTaskSource_ScraperTaskId",
                table: "ReportTaskSource",
                column: "ScraperTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportTaskRecipient");

            migrationBuilder.DropTable(
                name: "ReportTaskSource");

            migrationBuilder.DropIndex(
                name: "IX_Listing_ScraperTaskId_RemovedAt",
                table: "Listing");

            migrationBuilder.DropColumn(
                name: "LastSuccessfulReportAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "Listing");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Tasks",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34);

            migrationBuilder.CreateIndex(
                name: "IX_Listing_ScraperTaskId",
                table: "Listing",
                column: "ScraperTaskId");
        }
    }
}
