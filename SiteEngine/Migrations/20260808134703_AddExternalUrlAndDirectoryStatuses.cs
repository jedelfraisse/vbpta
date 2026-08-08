using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalUrlAndDirectoryStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "Sites");
        }
    }
}
