using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SiteEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstLoginUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastLoginSiteId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoginCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoginHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LoginUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NetworkType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoginHistory_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistorySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SchoolYear = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalLogins = table.Column<int>(type: "int", nullable: false),
                    SchoolNetworkLogins = table.Column<int>(type: "int", nullable: false),
                    HomeNetworkLogins = table.Column<int>(type: "int", nullable: false),
                    MobileNetworkLogins = table.Column<int>(type: "int", nullable: false),
                    UniqueSites = table.Column<int>(type: "int", nullable: false),
                    FirstLoginUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastLoginUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistorySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistorySummaries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoginHistorySummaries_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LastLoginSiteId",
                table: "AspNetUsers",
                column: "LastLoginSiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_LoginUtc",
                table: "LoginHistory",
                column: "LoginUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_SiteId",
                table: "LoginHistory",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistory_UserId",
                table: "LoginHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistorySummaries_SiteId",
                table: "LoginHistorySummaries",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistorySummaries_UserId_SchoolYear",
                table: "LoginHistorySummaries",
                columns: new[] { "UserId", "SchoolYear" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Sites_LastLoginSiteId",
                table: "AspNetUsers",
                column: "LastLoginSiteId",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Sites_LastLoginSiteId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "LoginHistory");

            migrationBuilder.DropTable(
                name: "LoginHistorySummaries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LastLoginSiteId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstLoginUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginSiteId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LoginCount",
                table: "AspNetUsers");
        }
    }
}
