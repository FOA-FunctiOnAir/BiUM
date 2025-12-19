using BiUM.Core.Common.Behaviours.Caching.Redis;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IApplicationBuilder UseCore(this IApplicationBuilder app)
    {
        app.UseMiddleware<PerformanceMiddleware>();

        return app;
    }
}