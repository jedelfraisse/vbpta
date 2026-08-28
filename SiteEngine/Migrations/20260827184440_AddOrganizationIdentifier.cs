using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentifierLabel",
                table: "OrganizationTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentifierRequirement",
                table: "OrganizationTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IdentifierValue",
                table: "Organizations",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationTypeId_IdentifierValue",
                table: "Organizations",
                columns: new[] { "OrganizationTypeId", "IdentifierValue" },
                unique: true,
                filter: "[IdentifierValue] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_OrganizationTypeId_IdentifierValue",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IdentifierLabel",
                table: "OrganizationTypes");

            migrationBuilder.DropColumn(
                name: "IdentifierRequirement",
                table: "OrganizationTypes");

            migrationBuilder.DropColumn(
                name: "IdentifierValue",
                table: "Organizations");
        }
    }
}
