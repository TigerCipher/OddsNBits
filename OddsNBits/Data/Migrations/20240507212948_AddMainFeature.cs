using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OddsNBits.Migrations
{
    /// <inheritdoc />
    public partial class AddMainFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMainFeature",
                table: "BlogPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMainFeature",
                table: "BlogPosts");
        }
    }
}
