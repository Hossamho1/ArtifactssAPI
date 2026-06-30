using System.ComponentModel.DataAnnotations;

namespace ArtifactsAPI.Application.DTOs
{
    public class LoginDTOs
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;
    }
}