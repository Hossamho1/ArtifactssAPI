namespace ArtifactsAPI.Application.DTOs;

public class CreateAIReportDto
{
    public int ArtifactId { get; set; }
    public IFormFile Image { get; set; } = null!; 
    public string? Temperature { get; set; }
    public string? Humidity { get; set; }

}
