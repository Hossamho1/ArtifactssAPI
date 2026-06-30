using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ArtifactsAPI.Migrations
{
    public partial class FixArtifactIdForExistingPosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Insert a default artifact
            migrationBuilder.Sql(@"
                INSERT INTO ""Artifacts"" (""Name"", ""History"", ""Location"")
                VALUES ('Default Artifact', 'Auto-generated for legacy posts', 'Unknown');
            ");

            // 2. Update all existing posts to reference the default artifact
            migrationBuilder.Sql(@"
                UPDATE ""Posts"" SET ""ArtifactId"" = (SELECT ""Id"" FROM ""Artifacts"" WHERE ""Name"" = 'Default Artifact' LIMIT 1)
                WHERE ""ArtifactId"" = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optionally: Remove the default artifact if no posts reference it
            migrationBuilder.Sql(@"
                DELETE FROM ""Artifacts"" WHERE ""Name"" = 'Default Artifact' AND ""Id"" NOT IN (SELECT ""ArtifactId"" FROM ""Posts"");
            ");
        }
    }
}
