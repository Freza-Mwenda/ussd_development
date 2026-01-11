using Microsoft.EntityFrameworkCore;
using Npgsql;
using UssdDevelopmentCore.Database;

namespace UssdDevelopmentCore.Extensions;

public static class DatabaseExtensions
{
    public static void AddPostgresDatabase(this IServiceCollection service, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString).EnableUnmappedTypes();
        dataSourceBuilder.EnableDynamicJson();
        dataSourceBuilder.RegisterEnumTypeConversion();
        
        var dataSource = dataSourceBuilder.Build();
        
        service.AddDbContext<NasdacDatabase>(options =>
        {
            options.UseNpgsql(dataSource, opt => opt
                    .EnableRetryOnFailure()
                    .MigrationsHistoryTable("__EFMigrationsHistory", "public"))
                .UseSnakeCaseNamingConvention();
        });
    }
}