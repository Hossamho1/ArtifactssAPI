using Microsoft.EntityFrameworkCore;
using ArtifactsAPI.Domain.Models;

namespace ArtifactsAPI.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Artifact> Artifacts { get; set; }
    public DbSet<ScanRecord> ScanRecords { get; set; }
    public DbSet<AIReport> AIReports { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Coordinate> Coordinates { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<PostView> PostViews { get; set; }


}

