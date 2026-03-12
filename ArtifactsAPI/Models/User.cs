using System.ComponentModel.DataAnnotations;

namespace ArtifactsAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Tourist or Engineer

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}
