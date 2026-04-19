using ArtifactsAPI.Models; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; 
using ArtifactsAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // 2. Create a New Post (Locked for Engineers only)
        [HttpPost]
        [Authorize(Roles = "Engineer")]
        public async Task<ActionResult<Post>> CreatePost(Post newPost)
        {
            //  Securely extract the User ID from the JWT token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User could not be identified from the token.");
            }

            // Attach the logged-in User's ID to the post
            newPost.UserId = userId;

            // Entity Framework is smart: If Flutter sends coordinates inside the post, it saves them all in one go!
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPosts), new { id = newPost.Id }, newPost);
        }
    }
}