using Microsoft.AspNetCore.Http;

namespace ArtifactsAPI.Application.DTOs
{
    public class CreatePostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // The secret here: We receive actual physical files instead of just text (Base64)
        public IFormFile? CoverPhoto { get; set; }
        public IFormFile? Model3D { get; set; }

        // The mobile app will send the Coordinates as a JSON String
        public string? CoordinatesJson { get; set; }
    }
}