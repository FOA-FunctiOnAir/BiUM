using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureApps
{
    public static WebApplication AddDomainInfrastructureApps(this WebApplication app)
    {
        return app;
    }
}
