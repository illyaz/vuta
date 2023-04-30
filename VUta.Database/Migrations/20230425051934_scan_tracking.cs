using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class scan_tracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_comment_scan",
                table: "videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_update",
                table: "videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_comment_scan",
                table: "videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_update",
                table: "videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_update",
                table: "channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_video_scan",
                table: "channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_update",
                table: "channels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_video_scan",
                table: "channels",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_comment_scan",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "last_update",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "next_comment_scan",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "next_update",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "last_update",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "last_video_scan",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "next_update",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "next_video_scan",
                table: "channels");
        }
    }
}
