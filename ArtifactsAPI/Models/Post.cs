using System.Collections.Generic;

namespace ArtifactsAPI.Models
{
    public class Post : BaseEntity 
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CoverPhoto { get; set; } = string.Empty;
        public string Model3D { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        //Composition 

        public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
    }
}