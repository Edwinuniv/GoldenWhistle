using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldenWhistle.Migrations
{
    /// <inheritdoc />
    public partial class addMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stage",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stage",
                table: "Matches");
        }
    }
}
