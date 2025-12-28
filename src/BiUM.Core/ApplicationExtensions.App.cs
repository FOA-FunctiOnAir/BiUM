using BiUM.Core.Common.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IApplicationBuilder UseCore(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestDurationMiddleware>();

        return app;
    }
}
