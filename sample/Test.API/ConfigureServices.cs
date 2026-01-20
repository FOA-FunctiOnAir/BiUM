using BiApp.Test.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BiApp.Test.API;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainAPIServices(this IServiceCollection services)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddHealthChecks().AddDbContextCheck<TestDbContext>();

        return services;
    }
}
