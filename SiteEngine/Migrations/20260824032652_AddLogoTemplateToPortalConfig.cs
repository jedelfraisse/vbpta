using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoTemplateToPortalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LogoTemplateBoxHeightPct",
                table: "PortalConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LogoTemplateBoxWidthPct",
                table: "PortalConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LogoTemplateBoxXPct",
                table: "PortalConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LogoTemplateBoxYPct",
                table: "PortalConfig",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "LogoTemplateFontColor",
                table: "PortalConfig",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoTemplateFontFamily",
                table: "PortalConfig",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoTemplateTextAlign",
                table: "PortalConfig",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoTemplateUrl",
                table: "PortalConfig",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoTemplateBoxHeightPct",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateBoxWidthPct",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateBoxXPct",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateBoxYPct",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateFontColor",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateFontFamily",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateTextAlign",
                table: "PortalConfig");

            migrationBuilder.DropColumn(
                name: "LogoTemplateUrl",
                table: "PortalConfig");
        }
    }
}
