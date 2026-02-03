using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAircraftModeS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModeS",
                table: "Aircraft",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeS",
                table: "Aircraft");
        }
    }
}
