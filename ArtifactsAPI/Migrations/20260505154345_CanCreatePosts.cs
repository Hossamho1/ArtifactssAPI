using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtifactsAPI.Migrations
{
    /// <inheritdoc />
    public partial class CanCreatePosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanCreatePosts",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanCreatePosts",
                table: "Users");
        }
    }
}
