using BiApp.Test.Application.Repositories;
using BiApp.Test.Infrastructure.Persistence;
using BiApp.Test.Infrastructure.Repositories;
using BiUM.Core.Common.Exceptions;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        _ = services.AddScoped<ITestDbContext>(sp => sp.GetRequiredService<TestDbContext>());

        _ = services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        _ = services.AddScoped<IBoltDbContext>(sp => sp.GetRequiredService<BoltDbContext>());

        _ = services.AddScoped<ICurrencyRepository, CurrencyRepository>();

        return services;
    }

    public static async Task SyncAll(this IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();

            await scope.ServiceProvider.InitialiseDatabase();

            await scope.ServiceProvider.SyncBolt();

            await scope.ServiceProvider.SyncDatabase();
        }
        catch (ApplicationStartupException)
        {
            throw;
        }
        catch
        {
        }
    }
}