using ArtifactsAPI.Domain.Models;

public class Post : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CoverPhoto { get; set; } = string.Empty;
    public string Model3D { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User? User { get; set; }

    public int ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;

    public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
}