using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    // Hand-corrected after scaffolding — see comments below for what was wrong
    // with the auto-generated version and why.
    public partial class AddPerLogoMastheadSizing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The scaffolded migration paired these with the new
            // PtaVariantLogoWidth/Height columns via RenameColumn, because
            // both are same-typed int? columns and EF's diff guessed a
            // rename. That's wrong: MastheadLogoMaxWidth/Height (added last
            // migration, effectively unused so far) are being replaced by
            // MastheadLogoDefaultWidth/Height below — PtaVariantLogoWidth/
            // Height are a genuinely new, separate field. A rename here would
            // have silently moved any admin-entered "default box" value into
            // the "PTA variant logo" slot instead.
            migrationBuilder.DropColumn(name: "MastheadLogoMaxWidth", table: "Sites");
            migrationBuilder.DropColumn(name: "MastheadLogoMaxHeight", table: "Sites");

            migrationBuilder.AddColumn<int>(name: "MastheadLogoDefaultWidth", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "MastheadLogoDefaultHeight", table: "Sites", type: "int", nullable: true);

            migrationBuilder.AddColumn<int>(name: "GeneratedLogoWidth", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "GeneratedLogoHeight", table: "Sites", type: "int", nullable: true);
            // The scaffolded migration backfilled all 4 PreserveAspectRatio
            // columns with defaultValue: false. That's the CLR default for
            // bool, not the "= true" the Site.cs property initializer
            // actually declares — property initializers only apply to
            // objects EF constructs, not to a migration's backfill of
            // existing rows. Explicit `true` here keeps already-existing
            // sites on the safe "don't distort my logo" default until an
            // admin deliberately unchecks it, matching every new site.
            migrationBuilder.AddColumn<bool>(name: "GeneratedLogoPreserveAspectRatio", table: "Sites", type: "bit", nullable: false, defaultValue: true);

            migrationBuilder.AddColumn<int>(name: "PtaVariantLogoWidth", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PtaVariantLogoHeight", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "PtaVariantLogoPreserveAspectRatio", table: "Sites", type: "bit", nullable: false, defaultValue: true);

            migrationBuilder.AddColumn<int>(name: "DistrictLogoWidth", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "DistrictLogoHeight", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "DistrictLogoPreserveAspectRatio", table: "Sites", type: "bit", nullable: false, defaultValue: true);

            migrationBuilder.AddColumn<int>(name: "PartnerLogoWidth", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PartnerLogoHeight", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "PartnerLogoPreserveAspectRatio", table: "Sites", type: "bit", nullable: false, defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PartnerLogoPreserveAspectRatio", table: "Sites");
            migrationBuilder.DropColumn(name: "PartnerLogoHeight", table: "Sites");
            migrationBuilder.DropColumn(name: "PartnerLogoWidth", table: "Sites");

            migrationBuilder.DropColumn(name: "DistrictLogoPreserveAspectRatio", table: "Sites");
            migrationBuilder.DropColumn(name: "DistrictLogoHeight", table: "Sites");
            migrationBuilder.DropColumn(name: "DistrictLogoWidth", table: "Sites");

            migrationBuilder.DropColumn(name: "PtaVariantLogoPreserveAspectRatio", table: "Sites");
            migrationBuilder.DropColumn(name: "PtaVariantLogoHeight", table: "Sites");
            migrationBuilder.DropColumn(name: "PtaVariantLogoWidth", table: "Sites");

            migrationBuilder.DropColumn(name: "GeneratedLogoPreserveAspectRatio", table: "Sites");
            migrationBuilder.DropColumn(name: "GeneratedLogoHeight", table: "Sites");
            migrationBuilder.DropColumn(name: "GeneratedLogoWidth", table: "Sites");

            migrationBuilder.DropColumn(name: "MastheadLogoDefaultHeight", table: "Sites");
            migrationBuilder.DropColumn(name: "MastheadLogoDefaultWidth", table: "Sites");

            migrationBuilder.AddColumn<int>(name: "MastheadLogoMaxHeight", table: "Sites", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "MastheadLogoMaxWidth", table: "Sites", type: "int", nullable: true);
        }
    }
}
