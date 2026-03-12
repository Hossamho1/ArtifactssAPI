using Microsoft.EntityFrameworkCore;
using ArtifactsAPI.Models; // تأكد إن ده نفس الـ namespace بتاع الـ Models بتاعتك

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
}
