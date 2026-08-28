using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTypes", x => x.Id);
                });

            // SeedData.EnsureSeedData (which also inserts this same row) only
            // ever runs from the Setup Wizard's "configure a new database"
            // flow (SetupService.RunMigrationsAsync) — ordinary startup only
            // calls Database.MigrateAsync(), never that seeding step. For an
            // already-configured database (dev/stage/live), that means the
            // table would otherwise get created empty and stay that way
            // forever. Migrations DO run on every normal startup, so the
            // initial row belongs here too, not just in the C# seed helper.
            migrationBuilder.InsertData(
                table: "OrganizationTypes",
                columns: new[] { "Name", "Description", "IconClass", "SortOrder" },
                values: new object[]
                {
                    "PTA",
                    "Parent Teacher Associations connect families, teachers, and schools — " +
                        "organizing everything from fundraisers and family events to advocacy " +
                        "for students, all built on the National PTA's everychild.onevoice. " +
                        "mission. A PTA is organized as a Division (e.g. a citywide or regional " +
                        "council) made up of Local Units (individual school PTAs), each with " +
                        "its own site on this portal.",
                    "fa-solid fa-people-roof",
                    1,
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationTypes");
        }
    }
}
