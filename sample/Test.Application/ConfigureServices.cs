using BiApp.Test.Application;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddDomainApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationAdditionalServices<IApplicationMarker>(configuration);

        return services;
    }
}
