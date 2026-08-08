using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddPtaLogoGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PartnerLogoUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<string>(
                name: "DistrictLogoUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PTALogoVariantUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolCrestUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictLogoUrl",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "PTALogoVariantUrl",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "SchoolCrestUrl",
                table: "Sites");

            migrationBuilder.AlterColumn<string>(
                name: "PartnerLogoUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "Sites",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
