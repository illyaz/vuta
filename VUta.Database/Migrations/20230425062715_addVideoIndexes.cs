using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class addVideoIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
