using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BiUM.Test.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureAdditionalServices(configuration, Assembly.GetExecutingAssembly());

        return services;
    }
}