using ArtifactsAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtifactsAPI.Infrastructure.Configurations;

public class AIReportConfiguration : IEntityTypeConfiguration<AIReport>
{
    public void Configure(EntityTypeBuilder<AIReport> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Severity).HasColumnType("varchar(50)");
        builder.Property(r => r.Temperature).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Humidity).HasColumnType("decimal(18,2)");
    }
}