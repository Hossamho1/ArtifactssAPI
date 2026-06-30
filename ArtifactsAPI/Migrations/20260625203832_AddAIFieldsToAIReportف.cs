using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtifactsAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFieldsToAIReportف : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrackSeverity",
                table: "AIReports");

            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "AIReports",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Severity",
                table: "AIReports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddColumn<decimal>(
                name: "CrackSeverity",
                table: "AIReports",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
