using Microsoft.AspNetCore.Builder;

namespace BiApp.Test2.Infrastructure;

public static class ConfigureApps
{
    public static WebApplication AddDomainInfrastructureApps(this WebApplication app)
    {
        return app;
    }
}
