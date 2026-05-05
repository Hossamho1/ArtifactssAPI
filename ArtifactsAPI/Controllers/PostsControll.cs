using ArtifactsAPI.Data;
using ArtifactsAPI.DTOs;
using ArtifactsAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
namespace ArtifactsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Get All Posts (Available to everyone)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            // Fetch posts along with their coordinates AND the user's info (so Flutter can show the author's name/photo)
            var posts = await _context.Posts
                .Include(p => p.Coordinates) // Get the list of coordinates
                .Include(p => p.User)        // Get the author's data
                .OrderByDescending(p => p.Id) // Show the newest posts first
                .ToListAsync();

            return Ok(posts);
        }






        [HttpPost("create")]
        [Authorize]
        [RequestSizeLimit(104857600)] // Allows up to 100MB for the large .glb files
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
        {
            // 1. Securely extract UserId from the JWT Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("User ID not found in token.");
            }
            int currentUserId = int.Parse(userIdClaim);

            // TODO: Add file saving logic here for request.CoverPhoto & request.Model3D
            // Example: string savedPhotoPath = await SaveFile(request.CoverPhoto);
            string placeholderPhotoPath = "";
            string placeholderModelPath = "";

            // 2. Map the DTO to the pure Domain Entity (Post)
            var newPost = new Post
            {
                Title = request.Title,
                Description = request.Description,
                CoverPhoto = placeholderPhotoPath, // Save the path string, not the file
                Model3D = placeholderModelPath,    // Save the path string, not the file
                UserId = currentUserId
            };

            // 3. Deserialize the Coordinates JSON string into a List<Coordinate>
            if (!string.IsNullOrEmpty(request.CoordinatesJson))
            {
                try
                {
                    newPost.Coordinates = JsonSerializer.Deserialize<List<Coordinate>>(request.CoordinatesJson) ?? new List<Coordinate>();
                }
                catch (JsonException)
                {
                    return BadRequest("Invalid JSON format for coordinates.");
                }
            }

            // 4. Save to the database
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Post created successfully!", PostId = newPost.Id });
        }



    }
}