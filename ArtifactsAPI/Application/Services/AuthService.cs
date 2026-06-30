using ArtifactsAPI.Application.DTOs;
using ArtifactsAPI.Application.Interfaces;

using ArtifactsAPI.Domain.Models;
using ArtifactsAPI.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ArtifactsAPI.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IValidator<LoginDTOs> _loginValidator;
        private readonly IValidator<User> _registerValidator;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment env, IValidator<LoginDTOs> loginValidator, IValidator<User> registerValidator)
        {
            _context = context;
            _configuration = configuration;
            _env = env;
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
        }


        /// <summary>
        /// Authenticates a user with email and password, and generates a JWT token on successful login.
        /// </summary>
        /// <param name="loginDto">Login credentials (email and password)</param>
        /// <returns>Tuple with success status, message, and user data with token if successful</returns>
        public async Task<(bool IsSuccess, string Message, object Data)> LoginAsync(LoginDTOs loginDto)
        {
            var validationResult = await _loginValidator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return (false, errors, null);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.PasswordHash, user.PasswordHash))
                return (false, "Invalid Email or Password.", null);

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

            var userData = new { Token = tokenString, user.Id, user.Name, user.Role, user.CanCreatePosts };
            return (true, "Login successful", userData);
        }


        /// <summary>
        /// Registers a new user with email validation and password hashing using BCrypt.
        /// </summary>
        /// <param name="userDto">User data including email and password</param>
        /// <returns>Tuple with success status and confirmation message</returns>
        public async Task<(bool IsSuccess, string Message)> RegisterAsync(User userDto)
        {
            var validationResult = await _registerValidator.ValidateAsync(userDto);
            if (!validationResult.IsValid)
                return (false, validationResult.Errors.First().ErrorMessage);

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (existingUser != null) return (false, "This email is already registered!");

            userDto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.PasswordHash);
            _context.Users.Add(userDto);
            await _context.SaveChangesAsync();

            return (true, "User registered successfully with hashed password!");
        }

        /// <summary>
        /// Retrieves all users with the Engineer role.
        /// </summary>
        /// <returns>Collection of all engineers</returns>
        public async Task<IEnumerable<User>> GetAllEngineersAsync()
        {
            return await _context.Users.Where(u => u.Role == "Engineer").ToListAsync();
        }

        /// <summary>
        /// Grants or revokes post creation permission for an engineer by user ID or email.
        /// Prevents code duplication for permission management.
        /// </summary>
        /// <param name="userId">User ID (optional if email is provided)</param>
        /// <param name="email">User email (optional if userId is provided)</param>
        /// <param name="grantPermission">True to grant permission; false to revoke</param>
        /// <returns>Tuple with success status and action message</returns>
        public async Task<(bool IsSuccess, string Message)> ChangeEngineerPermissionAsync(int? userId, string email, bool grantPermission)
        {
            var targetUser = userId.HasValue
                ? await _context.Users.FindAsync(userId.Value)
                : await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (targetUser == null) return (false, "User not found.");
            if (targetUser.Role != "Engineer") return (false, "Invalid selection. You can only modify permissions for an Engineer.");

            if (grantPermission && targetUser.CanCreatePosts) return (false, "This engineer already has permission.");
            if (!grantPermission && !targetUser.CanCreatePosts) return (false, "This engineer already does NOT have permission.");

            targetUser.CanCreatePosts = grantPermission;
            await _context.SaveChangesAsync();

            string action = grantPermission ? "granted to" : "revoked from";
            return (true, $"Post creation permission {action}: {targetUser.Email}");
        }

        /// <summary>
        /// Deletes a user account from the database by email address.
        /// </summary>
        /// <param name="email">The email address of the user to delete</param>
        /// <returns>Tuple with success status and confirmation message</returns>
        public async Task<(bool IsSuccess, string Message)> DeleteUserByEmailAsync(string email)
        {
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (targetUser == null) return (false, "User not found.");

            _context.Users.Remove(targetUser);
            await _context.SaveChangesAsync();
            return (true, $"User with email '{email}' has been successfully deleted.");
        }

        /// <summary>
        /// Uploads and saves a user's profile picture to the server.
        /// Generates a unique filename and stores the file URL in the user profile.
        /// </summary>
        /// <param name="userId">The ID of the user whose profile is being updated</param>
        /// <param name="file">The image file to upload</param>
        /// <param name="baseUrl">The base URL for constructing the file access URL</param>
        /// <returns>Tuple with success status, message, and the file URL if successful</returns>
        public async Task<(bool IsSuccess, string Message, string PhotoUrl)> UploadProfilePictureAsync(int userId, IFormFile file, string baseUrl)
        {
            if (file == null || file.Length == 0) return (false, "Please upload a valid image.", null);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.", null);

            string uploadsFolder = !string.IsNullOrEmpty(_env.WebRootPath)
                ? Path.Combine(_env.WebRootPath, "uploads", "profiles")
                : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var photoUrl = $"{baseUrl}/uploads/profiles/{uniqueFileName}";
            user.ProfilePicture = photoUrl;
            await _context.SaveChangesAsync();

            return (true, "Profile picture updated!", photoUrl);
        }

        /// <summary>
        /// Toggles the follow relationship between two users.
        /// If already following, unfollows the user; otherwise, creates a new follow relationship.
        /// </summary>
        /// <param name="currentUserId">The ID of the user initiating the follow action</param>
        /// <param name="targetUserId">The ID of the user to follow or unfollow</param>
        /// <returns>Tuple with success status and action message</returns>
        public async Task<(bool IsSuccess, string Message)> ToggleFollowAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId) return (false, "You cannot follow yourself.");

            var targetUser = await _context.Users.FindAsync(targetUserId);
            if (targetUser == null) return (false, "User not found.");

            var existingFollow = await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

            if (existingFollow != null)
            {
                _context.Follows.Remove(existingFollow);
                await _context.SaveChangesAsync();
                return (true, "Unfollowed successfully.");
            }

            _context.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = targetUserId });
            await _context.SaveChangesAsync();
            return (true, "Followed successfully.");
        }

        /// <summary>
        /// Retrieves a user's profile information including follower/following counts and follow status.
        /// </summary>
        /// <param name="userId">The ID of the user whose profile to retrieve</param>
        /// <param name="currentUserId">The ID of the current user (for determining follow status)</param>
        /// <returns>Tuple with success status, message, and profile data object if successful</returns>
        public async Task<(bool IsSuccess, string Message, object ProfileData)> GetUserProfileAsync(int userId, int currentUserId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.", null);

            int followersCount = await _context.Follows.CountAsync(f => f.FollowingId == userId);
            int followingCount = await _context.Follows.CountAsync(f => f.FollowerId == userId);
            bool isFollowedByMe = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

            var profileData = new
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePicture,
                CanCreatePosts = user.CanCreatePosts,
                FollowersCount = followersCount,
                FollowingCount = followingCount,
                IsFollowedByMe = isFollowedByMe
            };

            return (true, "Profile retrieved", profileData);
        }
    }
}