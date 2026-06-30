using ArtifactsAPI.Domain.Models;
using System.Text.Json.Serialization;

public class Artifact : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? History { get; set; }
    public string Location { get; set; } = string.Empty;

    public ICollection<ScanRecord> ScanRecords { get; set; } = new List<ScanRecord>();
    [JsonIgnore]
    public ICollection<AIReport> AIReports { get; set; } = new List<AIReport>();

}
