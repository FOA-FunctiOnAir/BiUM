using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureApp
{
    public static IApplicationBuilder AddInfrastructureApps(this IApplicationBuilder app)
    {
        // Configure Serilog logging
        app.UseSerilogRequestLogging();
        //app.UseSerilogExceptionHandler();

        return app;
    }
}