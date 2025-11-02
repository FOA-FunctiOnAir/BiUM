using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    public static IServiceCollection AddDatabase<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services, IConfiguration configuration
    )
        where TDbContext : DbContext, IDbContext
        where TDbContextInitialiser : class
    {
        if (configuration.GetValue<string>("DatabaseType") == "InMemory")
        {
            services.AddDbContext<TDbContext>(options =>
                options.UseInMemoryDatabase("InMemoryDb"));
        }
        else if (configuration.GetValue<string>("DatabaseType") == "MSSQL")
        {
            services.AddDbContext<TDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("MSSQL"),
                    builder => builder.MigrationsAssembly(typeof(TDbContext).Assembly.FullName)));
        }
        else if (configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("PostgreSQL"))
            {
                Pooling = true,
                MinPoolSize = 0,
                MaxPoolSize = 100,
                KeepAlive = 30
            };

            services.AddDbContext<TDbContext>(options =>
                options.UseNpgsql(
                    connectionStringBuilder.ConnectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                    }));
        }

        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.AddScoped(typeof(IDbContextInitialiser), typeof(TDbContextInitialiser));

        return services;
    }
}