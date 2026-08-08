using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteBrowseIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sites_SiteType_ParentSiteId_SiteName",
                table: "Sites",
                columns: new[] { "SiteType", "ParentSiteId", "SiteName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sites_SiteType_ParentSiteId_SiteName",
                table: "Sites");
        }
    }
}
