using ArtifactsAPI.Domain.Models;

public class Bookmark
{
    public int UserId { get; set; }
    public User User { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
}