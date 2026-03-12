using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtifactsAPI.Models
{
    public class AIReport
    {
        public int Id { get; set; }

        [Required]
        public int ArtifactId { get; set; } // Foreign Key

        [Required]
        public decimal CrackSeverity { get; set; }

        [Required]
        public decimal Temperature { get; set; }

        [Required]
        public decimal Humidity { get; set; }

        [Required]
        public DateTime Date { get; set; }

        // Navigation Property back to the Artifact
        [ForeignKey("ArtifactId")]
        public Artifact? Artifact { get; set; }
    }
}
