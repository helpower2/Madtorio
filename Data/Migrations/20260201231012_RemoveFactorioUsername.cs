using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madtorio.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFactorioUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactorioUsername",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FactorioUsername",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }
    }
}
