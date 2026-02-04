using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madtorio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ModTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ModUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Author = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DownloadsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModRequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ModRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModRequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModRequestLogs_ModRequests_ModRequestId",
                        column: x => x.ModRequestId,
                        principalTable: "ModRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModRequestVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VotedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModRequestVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModRequestVotes_ModRequests_ModRequestId",
                        column: x => x.ModRequestId,
                        principalTable: "ModRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModRequestLogs_ModRequestId",
                table: "ModRequestLogs",
                column: "ModRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModRequestVotes_ModRequestId_Username",
                table: "ModRequestVotes",
                columns: new[] { "ModRequestId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModRequestLogs");

            migrationBuilder.DropTable(
                name: "ModRequestVotes");

            migrationBuilder.DropTable(
                name: "ModRequests");
        }
    }
}
