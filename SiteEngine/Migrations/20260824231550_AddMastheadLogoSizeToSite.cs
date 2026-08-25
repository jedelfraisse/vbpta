using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMastheadLogoSizeToSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MastheadLogoMaxHeight",
                table: "Sites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MastheadLogoMaxWidth",
                table: "Sites",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MastheadLogoMaxHeight",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MastheadLogoMaxWidth",
                table: "Sites");
        }
    }
}
