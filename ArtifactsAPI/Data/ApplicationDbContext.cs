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
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<PostView> PostViews { get; set; }


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

        modelBuilder.Entity<PostLike>().HasKey(pl => new { pl.UserId, pl.PostId });
        modelBuilder.Entity<Bookmark>().HasKey(b => new { b.UserId, b.PostId });

        modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FollowingId });

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany()
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);  

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);


        //  Configure PostView relations 
        modelBuilder.Entity<PostView>().HasKey(pv => new { pv.PostId, pv.UserId });


    }
}

