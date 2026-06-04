using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRoutingAndGlobalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sites_Hostname",
                table: "Sites");

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                table: "Sites",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCityWide",
                table: "Sites",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PtaId",
                table: "Sites",
                type: "char(8)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GlobalConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RootDomain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PlatformDomain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SmtpPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UseSsl = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalConfig", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "GlobalConfig",
                columns: new[] { "Id", "PlatformDomain", "RootDomain", "SmtpHost", "SmtpPassword", "SmtpPort", "SmtpUsername", "UseSsl" },
                values: new object[] { 1, "localhost", "localhost", "", "", 587, "", true });

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("0f89ac2b-a0ac-40b8-b886-fd117e35903c"),
                columns: new[] { "Domain", "Hostname", "IsCityWide", "LogoUrl", "PtaId" },
                values: new object[] { "", "admin", false, "/images/vbpta-logo.png", "00000000" });

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"),
                columns: new[] { "Domain", "Hostname", "IsCityWide", "LogoUrl", "PtaId" },
                values: new object[] { "", "", true, "/images/vbpta-logo.png", "10000000" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Domain",
                table: "Sites",
                column: "Domain",
                unique: true,
                filter: "[Domain] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Hostname",
                table: "Sites",
                column: "Hostname",
                unique: true,
                filter: "[Hostname] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_PtaId",
                table: "Sites",
                column: "PtaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalConfig");

            migrationBuilder.DropIndex(
                name: "IX_Sites_Domain",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_Hostname",
                table: "Sites");

            migrationBuilder.DropIndex(
                name: "IX_Sites_PtaId",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "IsCityWide",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "PtaId",
                table: "Sites");

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("0f89ac2b-a0ac-40b8-b886-fd117e35903c"),
                columns: new[] { "Hostname", "LogoUrl" },
                values: new object[] { "admin.localhost", "/images/vbpta-logo-transparent.png" });

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: new Guid("2b30d683-ea4b-4e9e-b616-17a2198e3b79"),
                columns: new[] { "Hostname", "LogoUrl" },
                values: new object[] { "localhost", "/images/vbpta-logo-transparent.png" });

            migrationBuilder.CreateIndex(
                name: "IX_Sites_Hostname",
                table: "Sites",
                column: "Hostname",
                unique: true);
        }
    }
}
