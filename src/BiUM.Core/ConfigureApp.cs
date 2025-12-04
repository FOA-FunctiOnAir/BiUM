using BiUM.Core.Common.Behaviours.Caching.Redis;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureApp
{
    public static IApplicationBuilder AddCoreApps(this IApplicationBuilder app)
    {
        app.UseMiddleware<PerformanceMiddleware>();

        return app;
    }
}