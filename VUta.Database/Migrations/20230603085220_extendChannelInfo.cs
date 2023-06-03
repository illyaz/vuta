using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class extendChannelInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "banner",
                table: "channels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "subscriber_count",
                table: "channels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "video_count",
                table: "channels",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "banner",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "description",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "subscriber_count",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "video_count",
                table: "channels");
        }
    }
}
