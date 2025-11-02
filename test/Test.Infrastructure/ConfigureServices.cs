using BiUM.Test.Application.Repositories;
using BiUM.Test.Infrastructure.Persistence;
using BiUM.Test.Infrastructure.Repositories;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public async static Task<IServiceCollection> AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        services.AddScoped<ITestDbContext>(provider => provider.GetRequiredService<TestDbContext>());

        await services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        services.AddScoped<IBoltDbContext>(provider => provider.GetRequiredService<BoltDbContext>());

        services.AddScoped<ICurrencyRepository, CurrencyRepository>();

        return services;
    }

    public async static Task<bool> SyncAll(this IServiceProvider services)
    {
        try
        {
            // Initialise and seed database
            using var scope = services.CreateScope();

            await scope.InitialiseDatabase();

            await scope.SyncBolt();

            await scope.SyncDatabase();
        }
        catch { }

        return true;
    }
}