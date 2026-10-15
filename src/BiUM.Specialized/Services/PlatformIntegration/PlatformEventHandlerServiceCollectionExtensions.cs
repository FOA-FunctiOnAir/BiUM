using BiUM.Core.PlatformIntegration;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class PlatformEventHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IPlatformEventHandler).IsAssignableFrom(type));

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(typeof(IPlatformEventHandler), handlerType);
        }

        return services;
    }
}