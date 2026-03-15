using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace ArtifactsAPI.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Supabase Transaction Mode pooler (port 6543) with Npgsql pooling disabled.
        // PgBouncer transaction mode drops connections, requiring MaxAutoPrepare=0 
        const string connStr = "User Id=postgres.gcafyqgipywkoozavchi;Password=mOVUQVmXVPy5xtfB;Server=aws-1-eu-central-1.pooler.supabase.com;Port=6543;Database=postgres;Pooling=false;Trust Server Certificate=true";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
        dataSourceBuilder.ConnectionStringBuilder.MaxAutoPrepare = 0;
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(dataSource);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
