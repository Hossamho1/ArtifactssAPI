using ArtifactsAPI.Domain.Models;

public class Coordinate : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }

    public int PostId { get; set; }
    public Post? Post { get; set; }
}