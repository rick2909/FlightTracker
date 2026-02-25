using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacySharingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableActivityFeed",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfileVisibility",
                table: "UserPreferences",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowAirlines",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowCountries",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMapRoutes",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTotalMiles",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableActivityFeed",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowAirlines",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowCountries",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowMapRoutes",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ShowTotalMiles",
                table: "UserPreferences");
        }
    }
}
