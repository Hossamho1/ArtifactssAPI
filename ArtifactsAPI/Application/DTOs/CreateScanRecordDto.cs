using Microsoft.AspNetCore.Http;

namespace ArtifactsAPI.Application.DTOs;

public class CreateScanRecordDto
{
    public int ArtifactId { get; set; }

    public IFormFile ScanFile { get; set; }
}