using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultCitySite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Sites",
                columns: new[] { "Id", "AccentColor", "BannerUrl", "CreatedAtUtc", "Hostname", "IsAdminPortal", "LogoUrl", "PrimaryColor", "SiteName", "UpdatedAtUtc", "WelcomeText" },
                values: new object[] { new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"), "#FFCC00", "/images/TopBanner.png", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "localhost", false, "/images/vbpta-logo-transparent.png", "#003366", "Virginia Beach Council of PTAs", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Welcome to our community! We are dedicated to supporting students, families, and educators." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"));
        }
    }
}
