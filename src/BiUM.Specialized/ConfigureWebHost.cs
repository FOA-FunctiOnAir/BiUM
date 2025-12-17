using BiUM.Core.Common.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureWebHost
{
    public static WebApplicationBuilder AddSpecializedWebHost(this WebApplicationBuilder appBuilder)
    {
        var appOptions = appBuilder.Configuration.GetSection(BiAppOptions.Name).Get<BiAppOptions>();

        ArgumentNullException.ThrowIfNull(appOptions);

        var grpcOptions = appBuilder.Configuration.GetSection(BiGrpcOptions.Name).Get<BiGrpcOptions>();

        ArgumentNullException.ThrowIfNull(grpcOptions);

        var appPort = appOptions.Port > 0 ? appOptions.Port : 8080;

        appBuilder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(appPort, lo => lo.Protocols = HttpProtocols.Http1);

            if (grpcOptions.Enable)
            {
                var grpcPort = grpcOptions.Port > 0 ? grpcOptions.Port : appPort + 1000;

                options.ListenAnyIP(grpcPort, lo => lo.Protocols = HttpProtocols.Http2);
            }
        });

        return appBuilder;
    }
}