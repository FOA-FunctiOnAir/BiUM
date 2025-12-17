using BiUM.Core.Common.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureWebHost
{
    public static ConfigureWebHostBuilder AddSpecializedWebHost(this ConfigureWebHostBuilder webhost, IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var appOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>();
        var grpcOptions = serviceProvider.GetRequiredService<IOptions<BiGrpcOptions>>();

        var appPort = appOptions.Value.Port > 0 ? appOptions.Value.Port : 8080;
        var grpcPort = 0;
        var grpcProtocol = HttpProtocols.Http2;

        if (grpcOptions.Value.Enable && Enum.TryParse<HttpProtocols>(grpcOptions.Value.Protocol, out var _grpcProtocol))
        {
            grpcPort = grpcOptions.Value.Port;
            grpcProtocol = _grpcProtocol;
        }

        webhost.ConfigureKestrel(o =>
        {
            o.ListenAnyIP(appPort, lo => lo.Protocols = HttpProtocols.Http1);

            if (grpcOptions.Value.Enable && grpcPort > 0)
            {
                o.ListenAnyIP(grpcPort, lo => lo.Protocols = grpcProtocol);
            }
        });

        return webhost;
    }
}