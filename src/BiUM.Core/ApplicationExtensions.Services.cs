using BiUM.Core.Serialization;
using MemoryPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static WebApplicationBuilder ConfigureCoreServices(this WebApplicationBuilder builder)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();

        BiJsonOptions.Configure(jsonSerializerOptions);

        builder.Services.AddSingleton(jsonSerializerOptions);

        var memoryPackSerializerOptions = MemoryPackSerializerOptions.Default;

        builder.Services.AddSingleton(memoryPackSerializerOptions);

        builder.Services.AddScoped<CancellationTokenSource>(sp =>
        {
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();

            if (httpContextAccessor?.HttpContext is not null)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(httpContextAccessor.HttpContext.RequestAborted);
            }

            var hostApplicationLifetime = sp.GetRequiredService<IHostApplicationLifetime>();

            return CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping);
        });

        return builder;
    }
}