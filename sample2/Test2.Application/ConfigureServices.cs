using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiApp.Test2.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureAdditionalServices<IApplicationMarker>(configuration);

        return services;
    }
}
