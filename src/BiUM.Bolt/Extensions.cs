using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Exceptions;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static IServiceCollection AddBolt<TDbContext, TDbContextInitialiser>(
        this IServiceCollection services,
        IConfiguration configuration
    )
        where TDbContext : DbContext
        where TDbContextInitialiser : class, IBoltDbContextInitialiser
    {
        var section = configuration.GetSection(BoltOptions.Name);

        var boltOptions = section.Get<BoltOptions>() ?? throw new ApplicationStartupException(nameof(BoltOptions));

        if (!boltOptions.Enable)
        {
            return services;
        }

        services.Configure<BoltOptions>(section);

        var databaseName = BoltDatabaseExtensions.TryGetMainDatabaseName(configuration);

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return services;
        }

        var connectionString = string.Format(boltOptions.ConnectionString, databaseName);

        services.AddBoltDbContext<TDbContext>(configuration, connectionString);

        services.AddScoped<IBoltDbContextInitialiser, TDbContextInitialiser>();
        services.AddScoped<BoltEntitySaveChangesInterceptor>();

        services.AddHealthChecks().AddDbContextCheck<TDbContext>();

        return services;
    }
}