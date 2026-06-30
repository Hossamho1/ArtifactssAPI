using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtifactsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArtifactId",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ArtifactId",
                table: "Posts",
                column: "ArtifactId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Artifacts_ArtifactId",
                table: "Posts",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Artifacts_ArtifactId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ArtifactId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ArtifactId",
                table: "Posts");
        }
    }
}
