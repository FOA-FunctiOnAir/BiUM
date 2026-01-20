using BiApp.Test2.Application.Repositories;
using BiApp.Test2.Contract.Services.Rpc;
using BiApp.Test2.Infrastructure.Persistence;
using BiApp.Test2.Infrastructure.Repositories;
using BiUM.Infrastructure.MagicOnion.Client;
using BiUM.Specialized.Database;
using MagicOnion.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase<TestDbContext, TestDbContextInitialiser>(configuration);
        services.AddScoped<ITestDbContext>(sp => sp.GetRequiredService<TestDbContext>());

        services.AddBolt<BoltDbContext, DomainBoltDbContextInitialiser>(configuration);
        services.AddScoped<IBoltDbContext>(sp => sp.GetRequiredService<BoltDbContext>());

        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddRpcClient<ITestRpcService, RpcServicesClient>("test");

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

[MagicOnionClientGeneration(typeof(ITestRpcService), Serializer = MagicOnionClientGenerationAttribute.GenerateSerializerType.MemoryPack)]
public sealed partial class RpcServicesClient : IMagicOnionRpcClient<ITestRpcService>;
