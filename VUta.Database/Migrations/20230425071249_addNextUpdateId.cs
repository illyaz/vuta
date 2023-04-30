using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class addNextUpdateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "next_update_id",
                table: "videos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_videos_next_update_id",
                table: "videos",
                column: "next_update_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_videos_next_update_id",
                table: "videos");

            migrationBuilder.DropColumn(
                name: "next_update_id",
                table: "videos");
        }
    }
}
