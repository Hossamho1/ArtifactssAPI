using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtifactsAPI.Models
{
    public class ScanRecord
    {
        public int Id { get; set; }

        [Required]
        public int ArtifactId { get; set; } // Foreign Key

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ModelFileUrl { get; set; } = string.Empty;

        // Navigation Property back to the Artifact
        [ForeignKey("ArtifactId")]
        public Artifact? Artifact { get; set; }
    }
}
