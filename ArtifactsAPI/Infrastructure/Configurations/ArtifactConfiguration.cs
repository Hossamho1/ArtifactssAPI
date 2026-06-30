using ArtifactsAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtifactsAPI.Infrastructure.Configurations
{
    public class ArtifactConfiguration : IEntityTypeConfiguration<Artifact>
    {
        public void Configure(EntityTypeBuilder<Artifact> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Name).IsRequired().HasMaxLength(255);
            builder.Property(a => a.Location).IsRequired().HasMaxLength(255);

            // Relationships
            builder.HasMany(a => a.AIReports)
                   .WithOne(r => r.Artifact)
                   .HasForeignKey(r => r.ArtifactId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(a => a.ScanRecords)
                   .WithOne(s => s.Artifact)
                   .HasForeignKey(s => s.ArtifactId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}