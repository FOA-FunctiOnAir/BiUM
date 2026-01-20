using BiApp.Test2.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BiApp.Test2.API;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainAPIServices(this IServiceCollection services)
    {
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddHealthChecks().AddDbContextCheck<TestDbContext>();

        return services;
    }
}
