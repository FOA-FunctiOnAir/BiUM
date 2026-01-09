using BiUM.Specialized.Database;
using BiUM.Test.Contract;
using BiUM.Test2.Application.Repositories;
using BiUM.Test2.Infrastructure.Persistence;
using BiUM.Test2.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        _ = services.AddScoped<ITestDbContext>(provider => provider.GetRequiredService<TestDbContext>());

        _ = services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        _ = services.AddScoped<IBoltDbContext>(provider => provider.GetRequiredService<BoltDbContext>());

        _ = services.AddGrpcClient<TestApi.TestApiClient>(configuration, "test");

        _ = services.AddScoped<IAccountRepository, AccountRepository>();

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
