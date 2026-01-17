using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madtorio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModifiedDateToSaveFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "SaveFiles",
                type: "TEXT",
                nullable: true);

            // Backfill: Set ModifiedDate to UploadDate for existing records
            migrationBuilder.Sql(
                "UPDATE SaveFiles SET ModifiedDate = UploadDate WHERE ModifiedDate IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "SaveFiles");
        }
    }
}
