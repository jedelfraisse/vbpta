using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class EmailVerificationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("0f89ac2b-a0ac-40b8-b886-fd117e35903c"),
                columns: new[] { "BannerUrl", "LogoUrl", "SiteName", "WelcomeText" },
                values: new object[] { "/images/banner.png", "/images/logo.png", "City Wide PTA Admin", "Monitor and manage all PTA sites from the this admin portal." });

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"),
                columns: new[] { "BannerUrl", "LogoUrl" },
                values: new object[] { "/images/banner.png", "/images/logo.png" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("0f89ac2b-a0ac-40b8-b886-fd117e35903c"),
                columns: new[] { "BannerUrl", "LogoUrl", "SiteName", "WelcomeText" },
                values: new object[] { "/images/TopBanner.png", "/images/vbpta-logo.png", "VBPTA Admin", "Monitor and manage all VBPTA sites from the admin portal." });

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"),
                columns: new[] { "BannerUrl", "LogoUrl" },
                values: new object[] { "/images/TopBanner.png", "/images/vbpta-logo.png" });
        }
    }
}
