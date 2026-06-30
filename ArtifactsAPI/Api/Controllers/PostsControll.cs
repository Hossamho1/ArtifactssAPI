using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtifactsAPI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;

        public PostsController(IPostService postService)
        {
            _postService = postService;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        private string GetCurrentUserRole() => User.FindFirst(ClaimTypes.Role)?.Value;

        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            return Ok(await _postService.GetAllPostsAsync());
        }

        [HttpPost("create")]
        [Authorize]
        [RequestSizeLimit(104857600)]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized("User ID not found in token.");

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var result = await _postService.CreatePostAsync(request, userId, baseUrl);

            if (!result.IsSuccess) return StatusCode(403, new { Message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [HttpPut("edit/{id}")]
        [Authorize]
        public async Task<IActionResult> EditPost(int id, [FromBody] UpdatePostDto request)
        {
            var result = await _postService.EditPostAsync(id, request, GetCurrentUserId());
            if (!result.IsSuccess) return BadRequest(new { Message = result.ErrorMessage });

            return Ok(new { Message = "Post updated successfully!" });
        }

        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeletePost(int id)
        {
            var result = await _postService.DeletePostAsync(id, GetCurrentUserId(), GetCurrentUserRole());
            if (!result.IsSuccess) return StatusCode(403, new { Message = result.ErrorMessage });

            return Ok(new { Message = "Post deleted successfully!" });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPostById(int id)
        {
            var result = await _postService.GetPostByIdAsync(id, GetCurrentUserId());
            if (result == null) return NotFound(new { Message = "Post not found." });

            return Ok(result);
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var result = await _postService.ToggleLikeAsync(id, GetCurrentUserId());
            if (!result.IsSuccess) return NotFound(result.Message);

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{id}/bookmark")]
        [Authorize]
        public async Task<IActionResult> ToggleBookmark(int id)
        {
            var result = await _postService.ToggleBookmarkAsync(id, GetCurrentUserId());
            if (!result.IsSuccess) return NotFound(result.Message);

            return Ok(new { Message = result.Message });
        }

        [HttpPost("{id}/view")]
        [Authorize]
        public async Task<IActionResult> RecordPostView(int id)
        {
            var result = await _postService.RecordPostViewAsync(id, GetCurrentUserId());
            if (!result.IsSuccess) return NotFound(new { Message = result.Message });

            return Ok(new { Message = result.Message, ViewsCount = result.ViewsCount });
        }

    }
}