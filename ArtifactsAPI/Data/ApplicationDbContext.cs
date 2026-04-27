using Microsoft.EntityFrameworkCore;
using ArtifactsAPI.Models; 

namespace ArtifactsAPI.Data;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<ScanRecord> ScanRecords { get; set; }
    public DbSet<AIReport> AIReports { get; set; }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Coordinate> Coordinates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Post -> Coordinate relationship with cascade delete
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Coordinates)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Post -> User relationship
        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

