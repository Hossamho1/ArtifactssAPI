using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtifactsAPI.Domain.Models;

public class ScanRecord : BaseEntity
{
    public int ArtifactId { get; set; }
    public DateTime Date { get; set; }
    public string ModelFileUrl { get; set; } = string.Empty;

    public Artifact? Artifact { get; set; }
}