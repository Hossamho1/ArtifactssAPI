using System.ComponentModel.DataAnnotations;

namespace ArtifactsAPI.Models
{
    public class Artifact
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? History { get; set; } 

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        // Navigation Properties (1:N Relationships)
        public ICollection<ScanRecord> ScanRecords { get; set; } = new List<ScanRecord>();
        public ICollection<AIReport> AIReports { get; set; } = new List<AIReport>();
    }
}
