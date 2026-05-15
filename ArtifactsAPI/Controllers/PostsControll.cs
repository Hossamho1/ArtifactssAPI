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
        private readonly IWebHostEnvironment _env;

        public PostsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("User ID not found in token.");
            int currentUserId = int.Parse(userIdClaim);

            // 2. Check if the user exists and has permission from the Admin
            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null) return NotFound("User not found.");
            if (!user.CanCreatePosts)
                return StatusCode(403, new { Message = "Access Denied. An Admin must grant you permission to create posts." });

            //  New File Upload Logic 
            string finalPhotoUrl = "";
            string finalModelUrl = "";

            // Define the save directory path (wwwroot/uploads)
            string uploadsFolder = "";
            if (!string.IsNullOrEmpty(_env.WebRootPath))
            {
                uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            }
            else
            {
                uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            }

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 3. Save the Cover Photo
            if (request.CoverPhoto != null)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.CoverPhoto.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.CoverPhoto.CopyToAsync(fileStream);
                }
                // Generate the real URL to return to the mobile app
                finalPhotoUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}";
            }

            // 4. Save the 3D Model file
            if (request.Model3D != null)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + request.Model3D.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Model3D.CopyToAsync(fileStream);
                }
                // Generate the real URL to return to the mobile app
                finalModelUrl = $"{Request.Scheme}://{Request.Host}/uploads/{uniqueFileName}";
            }
            // -------------------------------------------------------------

            // 5. Map the DTO to the pure Domain Entity (Post)
            var newPost = new Post
            {
                Title = request.Title,
                Description = request.Description,
                CoverPhoto = finalPhotoUrl, // The real URL is saved here
                Model3D = finalModelUrl,    // The real URL is saved here
                UserId = currentUserId
            };

            // 6. Deserialize the Coordinates JSON string into a List<Coordinate>
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

            // 7. Save to the database
            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Post created successfully!",
                PostId = newPost.Id,
                CoverPhotoUrl = finalPhotoUrl,
                Model3DUrl = finalModelUrl
            });
        }

        [HttpPut("edit/{id}")]
        [Authorize]
        public async Task<IActionResult> EditPost(int id, [FromBody] UpdatePostDto request)
        {
            // 1. Get the ID of the logged-in user from the JWT Token
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // 2. Check if the post actually exists in the database
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { Message = "Post not found." });
            }

            // 3. STRICT OWNERSHIP CHECK: Is this user the creator of the post? 🌟
            if (post.UserId != currentUserId)
            {
                return StatusCode(403, new { Message = "Access Denied. You can only edit your own posts." });
            }

            // 4. Update the post data
            post.Title = request.Title ?? post.Title;
            post.Description = request.Description ?? post.Description;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Post updated successfully!" });
        }


        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound(new { Message = "Post not found." });
            }

           
            bool isOwner = (post.UserId == currentUserId); 
            bool isAdmin = (currentUserRole == "Admin");   

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new { Message = "Access Denied. You must be the owner of the post or an Admin to delete it." });
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Post deleted successfully!" });
        }
        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Post not found.");

            var existingLike = await _context.PostLikes.FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);
            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike); 
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Post Unliked" });
            }

            _context.PostLikes.Add(new PostLike { PostId = id, UserId = userId }); 
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Post Liked" });
        }

        [HttpPost("{id}/bookmark")]
        [Authorize]
        public async Task<IActionResult> ToggleBookmark(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound("Post not found.");

            var existingBookmark = await _context.Bookmarks.FirstOrDefaultAsync(b => b.PostId == id && b.UserId == userId);
            if (existingBookmark != null)
            {
                _context.Bookmarks.Remove(existingBookmark);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Bookmark removed" });
            }

            _context.Bookmarks.Add(new Bookmark { PostId = id, UserId = userId });
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Post Bookmarked" });
        }

        [HttpPost("{id}/view")]
        [Authorize]
        public async Task<IActionResult> RecordPostView(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            // 🌟 Unique View Check: Ensure the user hasn't already viewed this post today/ever 🌟
            var existingView = await _context.PostViews
                .FirstOrDefaultAsync(pv => pv.PostId == id && pv.UserId == currentUserId);

            if (existingView == null)
            {
                _context.PostViews.Add(new PostView { PostId = id, UserId = currentUserId });
                await _context.SaveChangesAsync();
            }

            int totalViews = await _context.PostViews.CountAsync(pv => pv.PostId == id);
            return Ok(new { Message = "View recorded successfully.", ViewsCount = totalViews });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPostById(int id)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound(new { Message = "Post not found." });

            var author = await _context.Users.FindAsync(post.UserId);

            // 🌟 🔥 Calculate ALL Counts Dynamically 🔥 🌟
            int totalLikes = await _context.PostLikes.CountAsync(l => l.PostId == id);
            int totalViews = await _context.PostViews.CountAsync(pv => pv.PostId == id); 

            bool isLikedByMe = await _context.PostLikes.AnyAsync(l => l.PostId == id && l.UserId == currentUserId);
            bool isBookmarkedByMe = await _context.Bookmarks.AnyAsync(b => b.PostId == id && b.UserId == currentUserId);

            return Ok(new
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CoverPhotoUrl = post.CoverPhoto,
                Model3DUrl = post.Model3D,
                Coordinates = post.Coordinates,

                Author = new
                {
                    Id = author?.Id,
                    Name = author?.Name ?? "Unknown Engineer",
                    ProfilePictureUrl = author?.ProfilePicture
                },

                
                LikesCount = totalLikes,
                ViewsCount = totalViews, 
                IsLiked = isLikedByMe,
                IsBookmarked = isBookmarkedByMe
            });
        }


    }
}