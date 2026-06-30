using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;
using ArtifactsAPI.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtifactsAPI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTOs loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (!result.IsSuccess) return Unauthorized(new { Message = result.Message });

            return Ok(result.Data);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User userDto)
        {
            var result = await _authService.RegisterAsync(userDto);
            if (!result.IsSuccess) return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("engineers")]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> GetAllEngineers()
        {
            var engineers = await _authService.GetAllEngineersAsync();
            return Ok(engineers);
        }

        // --- Admin Roles Management ---

        [HttpPut("select-engineer/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectEngineerById(int userId)
        {
            var result = await _authService.ChangeEngineerPermissionAsync(userId, null, grantPermission: true);
            return result.IsSuccess ? Ok(new { result.Message }) : BadRequest(new { result.Message });
        }

        [HttpPut("select-engineer/by-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SelectEngineerByEmail([FromQuery] string email)
        {
            var result = await _authService.ChangeEngineerPermissionAsync(null, email, grantPermission: true);
            return result.IsSuccess ? Ok(new { result.Message }) : BadRequest(new { result.Message });
        }

        [HttpPut("revoke-engineer/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeEngineerById(int userId)
        {
            var result = await _authService.ChangeEngineerPermissionAsync(userId, null, grantPermission: false);
            return result.IsSuccess ? Ok(new { result.Message }) : BadRequest(new { result.Message });
        }

        [HttpPut("revoke-engineer/by-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeEngineerByEmail([FromQuery] string email)
        {
            var result = await _authService.ChangeEngineerPermissionAsync(null, email, grantPermission: false);
            return result.IsSuccess ? Ok(new { result.Message }) : BadRequest(new { result.Message });
        }

        [HttpDelete("delete-user/by-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
        {
            var result = await _authService.DeleteUserByEmailAsync(email);
            return result.IsSuccess ? Ok(new { result.Message }) : NotFound(new { result.Message });
        }

        // --- Social & Profile Features ---

        [HttpPost("upload-profile-picture")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _authService.UploadProfilePictureAsync(GetCurrentUserId(), file, baseUrl);

            if (!result.IsSuccess) return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message, ProfilePictureUrl = result.PhotoUrl });
        }

        [HttpPost("users/{targetUserId}/follow")]
        [Authorize]
        public async Task<IActionResult> ToggleFollow(int targetUserId)
        {
            var result = await _authService.ToggleFollowAsync(GetCurrentUserId(), targetUserId);
            if (!result.IsSuccess) return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("users/{userId}/profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile(int userId)
        {
            var result = await _authService.GetUserProfileAsync(userId, GetCurrentUserId());
            if (!result.IsSuccess) return NotFound(new { Message = result.Message });

            return Ok(result.ProfileData);
        }
    }
}