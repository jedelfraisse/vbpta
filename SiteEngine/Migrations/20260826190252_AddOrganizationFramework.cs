using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                    CycleTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalCycles_OrganizationTypes_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    IsSiteEligible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationLevels_OrganizationTypes_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_OrganizationLevels_OrganizationLevelId",
                        column: x => x.OrganizationLevelId,
                        principalTable: "OrganizationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organizations_OrganizationTypes_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organizations_Organizations_ParentOrganizationId",
                        column: x => x.ParentOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organizations_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ParentAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentAccessGrants_Organizations_ChildOrganizationId",
                        column: x => x.ChildOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentAccessGrants_Organizations_ParentOrganizationId",
                        column: x => x.ParentOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalCycles_OrganizationTypeId",
                table: "OperationalCycles",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLevels_OrganizationTypeId_Name",
                table: "OrganizationLevels",
                columns: new[] { "OrganizationTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationLevelId",
                table: "Organizations",
                column: "OrganizationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationTypeId",
                table: "Organizations",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ParentOrganizationId",
                table: "Organizations",
                column: "ParentOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_SiteId",
                table: "Organizations",
                column: "SiteId",
                unique: true,
                filter: "[SiteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParentAccessGrants_ChildOrganizationId",
                table: "ParentAccessGrants",
                column: "ChildOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentAccessGrants_ParentOrganizationId_ChildOrganizationId",
                table: "ParentAccessGrants",
                columns: new[] { "ParentOrganizationId", "ChildOrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalCycles");

            migrationBuilder.DropTable(
                name: "ParentAccessGrants");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "OrganizationLevels");
        }
    }
}
