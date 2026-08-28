using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    // AddOrganizationType's own InsertData never actually seeded anything —
    // that migration had already been recorded as applied (dev database, and
    // presumably stage/live from the earlier deploy) by the time InsertData
    // was added to it. EF tracks migrations by id, not content, so editing an
    // already-applied migration file is a no-op; a genuinely new migration is
    // the only way to get this row inserted everywhere. Guarded with IF NOT
    // EXISTS instead of a plain InsertData so it's harmless to run against
    // any environment regardless of what that first migration did or didn't
    // insert there.
    public partial class SeedPtaOrganizationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Name = N'PTA')
BEGIN
    INSERT INTO OrganizationTypes (Name, Description, IconClass, SortOrder)
    VALUES (
        N'PTA',
        N'Parent Teacher Associations connect families, teachers, and schools — organizing everything from fundraisers and family events to advocacy for students, all built on the National PTA''s everychild.onevoice. mission. A PTA is organized as a Division (e.g. a citywide or regional council) made up of Local Units (individual school PTAs), each with its own site on this portal.',
        N'fa-solid fa-people-roof',
        1
    );
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM OrganizationTypes WHERE Name = N'PTA';");
        }
    }
}
