using ArtifactsAPI.Domain.Models;

public class AIReport : BaseEntity
{
    public int ArtifactId { get; set; }
    public bool HasCracks { get; set; }
    public float DamagePercentage { get; set; }
    public string Severity { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public decimal Humidity { get; set; }
    public DateTime Date { get; set; }

    public Artifact? Artifact { get; set; }
}