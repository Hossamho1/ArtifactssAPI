using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtifactsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFieldsToAIReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "DamagePercentage",
                table: "AIReports",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "HasCracks",
                table: "AIReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "AIReports",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamagePercentage",
                table: "AIReports");

            migrationBuilder.DropColumn(
                name: "HasCracks",
                table: "AIReports");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "AIReports");
        }
    }
}
