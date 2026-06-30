using ArtifactsAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ArtifactsAPI.Infrastructure.Configurations;

public class ScanRecordConfiguration : IEntityTypeConfiguration<ScanRecord>
{
    public void Configure(EntityTypeBuilder<ScanRecord> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ModelFileUrl).IsRequired().HasMaxLength(1000);
    }
}
