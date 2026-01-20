using BiApp.Test.Application.Repositories;
using BiApp.Test.Infrastructure.Persistence;
using BiApp.Test.Infrastructure.Repositories;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        services.AddScoped<ITestDbContext>(sp => sp.GetRequiredService<TestDbContext>());

        services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        services.AddScoped<IBoltDbContext>(sp => sp.GetRequiredService<BoltDbContext>());

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
