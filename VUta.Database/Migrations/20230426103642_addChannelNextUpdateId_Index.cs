using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class addChannelNextUpdateId_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "next_update_id",
                table: "channels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_channels_last_update",
                table: "channels",
                column: "last_update");

            migrationBuilder.CreateIndex(
                name: "ix_channels_last_video_scan",
                table: "channels",
                column: "last_video_scan");

            migrationBuilder.CreateIndex(
                name: "ix_channels_next_update",
                table: "channels",
                column: "next_update");

            migrationBuilder.CreateIndex(
                name: "ix_channels_next_update_id",
                table: "channels",
                column: "next_update_id");

            migrationBuilder.CreateIndex(
                name: "ix_channels_next_video_scan",
                table: "channels",
                column: "next_video_scan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_channels_last_update",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "ix_channels_last_video_scan",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "ix_channels_next_update",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "ix_channels_next_update_id",
                table: "channels");

            migrationBuilder.DropIndex(
                name: "ix_channels_next_video_scan",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "next_update_id",
                table: "channels");
        }
    }
}
