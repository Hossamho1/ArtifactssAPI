using Microsoft.EntityFrameworkCore;
using Npgsql;
// 👇 1. New libraries for JWT
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ArtifactsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // Supabase Transaction pooler (Port 6543) with internal pooling disabled.
            // PgBouncer drops connections between transactions, so Npgsql prepared 
            // statements MUST be disabled (MaxAutoPrepare = 0) to avoid fatal hanging 
            // on UPDATE/DELETE ops when the backend connection changes.
            var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
            dataSourceBuilder.ConnectionStringBuilder.MaxAutoPrepare = 0;
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<ArtifactsAPI.Data.ApplicationDbContext>(options =>
                options.UseNpgsql(dataSource));

            // JWT Authentication Configuration
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });
            // End of JWT Configuration

            builder.Services.AddOpenApi();

            builder.Services.AddCors(options => {
                options.AddPolicy("AllowAll", b => b.AllowAnyMethod()
                                                   .AllowAnyHeader()
                                                   .AllowAnyOrigin());
            });


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAll");

            // . Middleware Order is VERY important!
            app.UseAuthentication(); // First check WHO the user is (Token check)
            app.UseAuthorization();  // Then check WHAT they can do (Role check)

            app.MapControllers();

            // MapGet must be before app.Run()
            app.MapGet("/", () => "The API is running! Go to /swagger to test it.");

            app.Run();
        }
    }
}