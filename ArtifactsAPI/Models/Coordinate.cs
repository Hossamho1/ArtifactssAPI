using System.Text.Json.Serialization;

namespace ArtifactsAPI.Models
{
    public class Coordinate : BaseEntity 
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }

        public int PostId { get; set; }

        [JsonIgnore]
        public Post? Post { get; set; }
    }
}