using Microsoft.EntityFrameworkCore;

namespace ArtifactsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            // 1. رجعنا ربط الداتا بيز عشان الـ Controllers تشتغل
            builder.Services.AddDbContext<ArtifactsAPI.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

       

            app.Run();
        }
    }
}
