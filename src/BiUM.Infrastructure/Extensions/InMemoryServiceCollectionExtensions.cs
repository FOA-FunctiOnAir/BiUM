using BiUM.Core.Caching.InMemory;
using BiUM.Infrastructure.Services.Caching.InMemory;
using System.Linq;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddBiUMInMemoryClient(this IServiceCollection services)
    {
        services.AddMemoryCache();

        if (services.Any(d => d.ServiceType == typeof(IInMemoryClient) && !d.IsKeyedService))
        {
            return services;
        }

        services.AddSingleton<IInMemoryClient>(static sp =>
            new InMemoryClient(
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                sp.GetRequiredService<JsonSerializerOptions>()));

        return services;
    }
}