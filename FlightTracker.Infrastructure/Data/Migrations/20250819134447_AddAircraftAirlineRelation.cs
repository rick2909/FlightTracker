using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAircraftAirlineRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AirlineId",
                table: "Aircraft",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aircraft_AirlineId",
                table: "Aircraft",
                column: "AirlineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Aircraft_Airlines_AirlineId",
                table: "Aircraft",
                column: "AirlineId",
                principalTable: "Airlines",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aircraft_Airlines_AirlineId",
                table: "Aircraft");

            migrationBuilder.DropIndex(
                name: "IX_Aircraft_AirlineId",
                table: "Aircraft");

            migrationBuilder.DropColumn(
                name: "AirlineId",
                table: "Aircraft");
        }
    }
}
