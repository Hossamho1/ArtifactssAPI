using ArtifactsAPI.Data;
using ArtifactsAPI.DTOs;
using ArtifactsAPI.Models;
using BCrypt.Net;
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

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
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
            user.Role
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
}