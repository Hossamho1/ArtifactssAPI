using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Scalar.AspNetCore; // Ensure this package is installed via NuGet

namespace ArtifactsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();

            // --- Database Configuration (Supabase/PostgreSQL) ---
            // Using NpgsqlDataSource to handle transaction pooling (Port 6543)
            // MaxAutoPrepare is set to 0 to prevent issues with PgBouncer
            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
            dataSourceBuilder.ConnectionStringBuilder.MaxAutoPrepare = 0;
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<ArtifactsAPI.Data.ApplicationDbContext>(options =>
                options.UseNpgsql(dataSource));

            // --- JWT Authentication Setup ---
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                ),

                // Use the standard claim type constants so claims added when creating
                // the token (ClaimTypes.NameIdentifier / ClaimTypes.Role) are mapped
                // correctly into the authenticated principal.
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });

            // Add OpenAPI/Scalar support
            builder.Services.AddOpenApi();

            // Configure CORS to allow Flutter & AI team connections
            builder.Services.AddCors(options => {
                options.AddPolicy("AllowAll", b => b.AllowAnyMethod()
                                                   .AllowAnyHeader()
                                                   .AllowAnyOrigin());
            });

            var app = builder.Build();

            // --- API Documentation Setup ---
            // Moving these outside 'IsDevelopment' so they work on Railway (Production)
            app.MapOpenApi();
            app.MapScalarApiReference();

            // Middleware Pipeline
            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            // Authentication must come before Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Health Check / Welcome Route
            app.MapGet("/", () => "The Artifacts API is LIVE! Access documentation at: /scalar/v1");

            app.Run();
        }
    }
}