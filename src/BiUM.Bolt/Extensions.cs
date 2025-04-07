using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public async static Task<IServiceCollection> AddBolt<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services, IConfiguration configuration
    )
        where TDbContext : DbContext
        where TDbContextInitialiser : class
    {
        services.Configure<BoltOptions>(configuration.GetSection(BoltOptions.Name));

        var serviceProvider = services.BuildServiceProvider();
        var boltOptions = serviceProvider.GetRequiredService<IOptions<BoltOptions>>();
        var httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();

        if (configuration.GetValue<string>("DatabaseType") == "PostgreSQL")
        {
            var databaseName = string.Empty;
            var connectionStringArray = configuration.GetConnectionString("PostgreSQL")?.Split(";");

            if (connectionStringArray is null || connectionStringArray.Length == 0)
            {
                return services;
            }

            foreach (var connectionStringItem in connectionStringArray)
            {
                var connectionStringItems = connectionStringItem.Trim().Split("=");

                if (connectionStringItems[0] == "Database")
                {
                    databaseName = connectionStringItems[1].Trim();

                    break;
                }
            }

            //if (!string.IsNullOrEmpty(boltOptions.Value.Server))
            //{
            //    var parameters = new Dictionary<string, dynamic>
            //    {
            //        { "Branch", boltOptions.Value.Branch },
            //        { "DatabaseName", databaseName }
            //    };

            //    var responseBoltDbSave = await httpClientsService.Post<bool>(Guid.NewGuid(), default, Ids.Language.Turkish.Id, boltOptions.Value.Server, parameters, false);

            //    Console.WriteLine(JsonSerializer.Serialize(responseBoltDbSave));

            //    if (responseBoltDbSave == null || !responseBoltDbSave.Success)
            //    {
            //        // TODO: log
            //        Console.WriteLine(JsonSerializer.Serialize(responseBoltDbSave));
            //    }
            //}

            var connectionString = string.Format(boltOptions.Value.ConnectionString, boltOptions.Value.Branch + "_" + (databaseName ?? "db"));

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
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

        services.AddScoped(typeof(IBaseBoltDbContextInitialiser), typeof(TDbContextInitialiser));
        services.AddScoped<BoltEntitySaveChangesInterceptor>();

        return services;
    }
}