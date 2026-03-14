using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealityScraper.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CronExpression = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Listing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PriceFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScraperTaskId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listing_Tasks_ScraperTaskId",
                        column: x => x.ScraperTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperTaskRecipient",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperTaskRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperTaskRecipient_Tasks_ScraperTaskId",
                        column: x => x.ScraperTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperTaskTarget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScraperType = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperTaskTarget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScraperTaskTarget_Tasks_ScraperTaskId",
                        column: x => x.ScraperTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceHistory_Listing_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_ExternalId_ScraperTaskId",
                table: "Listing",
                columns: new[] { "ExternalId", "ScraperTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listing_ScraperTaskId",
                table: "Listing",
                column: "ScraperTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ListingId",
                table: "PriceHistory",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperTaskRecipient_ScraperTaskId",
                table: "ScraperTaskRecipient",
                column: "ScraperTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ScraperTaskTarget_ScraperTaskId",
                table: "ScraperTaskTarget",
                column: "ScraperTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "ScraperTaskRecipient");

            migrationBuilder.DropTable(
                name: "ScraperTaskTarget");

            migrationBuilder.DropTable(
                name: "Listing");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}
