using ArtifactsAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtifactsAPI.Infrastructure.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => new { f.FollowerId, f.FollowingId });

        builder.HasOne(f => f.Follower)
               .WithMany()
               .HasForeignKey(f => f.FollowerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Following)
               .WithMany()
               .HasForeignKey(f => f.FollowingId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}