using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMVC.NetApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAllTimeDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AllTimeDistanceMeters",
                table: "StravaAthletes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllTimeDistanceMeters",
                table: "StravaAthletes");
        }
    }
}
