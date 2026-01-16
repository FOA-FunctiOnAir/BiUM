using BiUM.Core.Common.Configs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    private const int DefaultPort = 8080;

    public static WebApplicationBuilder ConfigureSpecializedHost(this WebApplicationBuilder appBuilder)
    {
        var appOptions = appBuilder.Configuration.GetSection(BiAppOptions.Name).Get<BiAppOptions>();

        var appPort = appOptions?.Port > 0 ? appOptions.Port : DefaultPort;

        appBuilder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;

            options.ListenAnyIP(appPort, lo => lo.Protocols = HttpProtocols.Http1);
            options.ListenAnyIP(appPort + 1000, lo => lo.Protocols = HttpProtocols.Http2);
        });

        return appBuilder;
    }
}
