using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class reduceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_videos_channel_id",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_last_comment_scan",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_last_update",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_next_comment_scan",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_next_update",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_next_update_id",
                table: "videos");

            migrationBuilder.CreateIndex(
                name: "ix_videos_channel_id_publish_date",
                table: "videos",
                columns: new[] { "channel_id", "publish_date" });

            migrationBuilder.CreateIndex(
                name: "ix_videos_next_update_next_update_id",
                table: "videos",
                columns: new[] { "next_update", "next_update_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_videos_channel_id_publish_date",
                table: "videos");

            migrationBuilder.DropIndex(
                name: "ix_videos_next_update_next_update_id",
                table: "videos");

            migrationBuilder.CreateIndex(
                name: "ix_videos_channel_id",
                table: "videos",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_videos_last_comment_scan",
                table: "videos",
                column: "last_comment_scan");

            migrationBuilder.CreateIndex(
                name: "ix_videos_last_update",
                table: "videos",
                column: "last_update");

            migrationBuilder.CreateIndex(
                name: "ix_videos_next_comment_scan",
                table: "videos",
                column: "next_comment_scan");

            migrationBuilder.CreateIndex(
                name: "ix_videos_next_update",
                table: "videos",
                column: "next_update");

            migrationBuilder.CreateIndex(
                name: "ix_videos_next_update_id",
                table: "videos",
                column: "next_update_id");
        }
    }
}
