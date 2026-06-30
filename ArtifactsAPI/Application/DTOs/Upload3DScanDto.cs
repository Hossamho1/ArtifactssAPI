using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ArtifactsAPI.Application.DTOs;

public class Upload3DScanDto
{
    public List<IFormFile> Images { get; set; } = [];
    public int ArtifactId { get; set; }
}