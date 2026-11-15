using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace portfolio_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Woonplaats",
                table: "Tests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "leeftijd",
                table: "Tests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Woonplaats",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "leeftijd",
                table: "Tests");
        }
    }
}
