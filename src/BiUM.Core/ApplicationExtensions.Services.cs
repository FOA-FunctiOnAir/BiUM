using MemoryPack;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IServiceCollection ConfigureCoreServices(this IServiceCollection services, Assembly assembly)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        services.AddSingleton(jsonSerializerOptions);

        var memoryPackSerializerOptions = MemoryPackSerializerOptions.Default;

        services.AddSingleton(memoryPackSerializerOptions);

        services.AddScoped<CancellationTokenSource>(sp =>
        {
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();

            if (httpContextAccessor?.HttpContext is not null)
            {
                return CancellationTokenSource.CreateLinkedTokenSource(httpContextAccessor.HttpContext.RequestAborted);
            }

            var hostApplicationLifetime = sp.GetRequiredService<IHostApplicationLifetime>();

            return CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping);
        });

        return services;
    }
}
