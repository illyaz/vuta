using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VUta.Database.Migrations
{
    /// <inheritdoc />
    public partial class addChannelHandle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "handle",
                table: "channels",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_channels_handle",
                table: "channels",
                column: "handle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_channels_handle",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "handle",
                table: "channels");
        }
    }
}
