using BiUM.Specialized.Database;
using BiUM.Test.Application.Repositories;
using BiUM.Test.Infrastructure.Persistence;
using BiUM.Test.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        services.AddScoped<ITestDbContext>(provider => provider.GetRequiredService<TestDbContext>());

        services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        services.AddScoped<IBoltDbContext>(provider => provider.GetRequiredService<BoltDbContext>());

        services.AddScoped<ICurrencyRepository, CurrencyRepository>();

        return services;
    }

    public static async Task SyncAll(this IServiceProvider services)
    {
        try
        {
            // Initialise and seed database
            using var scope = services.CreateScope();

            await scope.ServiceProvider.InitialiseDatabase();

            await scope.ServiceProvider.SyncBolt();

            await scope.ServiceProvider.SyncDatabase();
        }
        catch
        {
            // ignore
        }
    }
}
