using Microsoft.EntityFrameworkCore;
using Npgsql;

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
