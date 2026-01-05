using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BiUM.Test.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddInfrastructureAdditionalServices<IApplicationMarker>(configuration);

        return services;
    }
}
