using ArtifactsAPI.Data;
using ArtifactsAPI.DTOs;
using ArtifactsAPI.Models;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtifactsAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration; 
    private readonly IWebHostEnvironment _env;

    public AuthController(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration configuration)
    {
        _context = context;
        _env = env;
        _configuration = configuration; 
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTOs loginDto)
    {
        // 1. Find the user by Email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        // 2. Verify the password
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.PasswordHash, user.PasswordHash))
        {
            return Unauthorized("Invalid Email or Password.");
        }

        // Generate JWT Token --- 
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new
        {
            Token = tokenString,
            user.Id,
            user.Name,
            user.Role,
            user.CanCreatePosts
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User userDto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        if (existingUser != null)
        {
            return BadRequest("This email is already registered!");
        }

        userDto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.PasswordHash);
        _context.Users.Add(userDto);
        await _context.SaveChangesAsync();

        return Ok("User registered successfully with hashed password!");
    }


    [HttpGet("engineers")]
    [Authorize(Roles = "Engineer")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllEngineers()
    {
        // Engineer
        var engineers = await _context.Users
            .Where(u => u.Role == "Engineer")
            .ToListAsync();

        return Ok(engineers);

    }

    [HttpPut("select-engineer/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SelectEngineerForPostsById(int userId)
    {
        var targetUser = await _context.Users.FindAsync(userId);

        if (targetUser == null)
        {
            return NotFound("User not found.");
        }

        if (targetUser.Role != "Engineer")
        {
            return BadRequest("Invalid selection. You can only grant this permission to an Engineer.");
        }

        if (targetUser.CanCreatePosts)
        {
            return BadRequest("This engineer already has permission to create posts.");
        }

        targetUser.CanCreatePosts = true;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Post creation permission granted to Engineer: {targetUser.Email} {targetUser.Name}" });
    }

    [HttpPut("select-engineer/by-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SelectEngineerForPostsByEmail([FromQuery] string email)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (targetUser == null)
        {
            return NotFound("User not found.");
        }

        if (targetUser.Role != "Engineer")
        {
            return BadRequest("Invalid selection. You can only grant this permission to an Engineer.");
        }

        if (targetUser.CanCreatePosts)
        {
            return BadRequest("This engineer already has permission to create posts.");
        }

        targetUser.CanCreatePosts = true;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Post creation permission granted to Engineer: {targetUser.Email} {targetUser.Name}" });
    }

    [HttpPut("revoke-engineer/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeEngineerPermission(int userId)
    {
        var targetUser = await _context.Users.FindAsync(userId);

        if (targetUser == null)
        {
            return NotFound("User not found.");
        }

        if (!targetUser.CanCreatePosts)
        {
            return BadRequest("This engineer already does NOT have permission to create posts.");
        }

        targetUser.CanCreatePosts = false;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Post creation permission revoked from: {targetUser.Email} {targetUser.Name}" });
    }

    [HttpPut("revoke-engineer/by-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevokeEngineerPermissionByEmail([FromQuery] string email)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (targetUser == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        if (!targetUser.CanCreatePosts)
        {
            return BadRequest(new { Message = "This engineer already does NOT have permission to create posts." });
        }

        targetUser.CanCreatePosts = false;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Post creation permission revoked from: {targetUser.Email}" });
    }

    [HttpDelete("delete-user/by-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUserByEmail([FromQuery] string email)
    {
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (targetUser == null)
        {
            return NotFound(new { Message = "User not found." });
        }

        _context.Users.Remove(targetUser);
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"User with email '{email}' has been successfully deleted from the system." });
    }


    [HttpPost("upload-profile-picture")]
    [Authorize]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Please upload a valid image.");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found.");

        string uploadsFolder = "";
        if (!string.IsNullOrEmpty(_env.WebRootPath))
        {
            uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
        }
        else
        {
            uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        }

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var photoUrl = $"{Request.Scheme}://{Request.Host}/uploads/profiles/{uniqueFileName}";
        user.ProfilePicture = photoUrl;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Profile picture updated!", ProfilePictureUrl = photoUrl });
    }

    [HttpPost("users/{targetUserId}/follow")]
    [Authorize]
    public async Task<IActionResult> ToggleFollow(int targetUserId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        // Prevent user from following themselves
        if (currentUserId == targetUserId)
            return BadRequest(new { Message = "You cannot follow yourself." });

        var targetUser = await _context.Users.FindAsync(targetUserId);
        if (targetUser == null) return NotFound(new { Message = "User not found." });

        var existingFollow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

        // If already following, Unfollow
        if (existingFollow != null)
        {
            _context.Follows.Remove(existingFollow);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Unfollowed successfully." });
        }

        // If not following, Follow
        _context.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = targetUserId });
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Followed successfully." });
    }


    [HttpGet("users/{userId}/profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile(int userId)
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound(new { Message = "User not found." });

        //  Calculate Followers & Following dynamically 
        int followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);
        int followingCount = await _context.Follows.CountAsync(f => f.FollowerId == userId);
        bool isFollowedByMe = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

        return Ok(new
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePicture,
            CanCreatePosts = user.CanCreatePosts,

            // Social Stats
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            IsFollowedByMe = isFollowedByMe
        });
    }



}
