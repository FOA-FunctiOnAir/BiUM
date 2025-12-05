using BiUM.Test.Infrastructure.GrpcServices;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureApps
{
    public static WebApplication AddDomainInfrastructureApps(this WebApplication app)
    {
        app.MapGrpcService<TestGrpcService>();

        return app;
    }
}