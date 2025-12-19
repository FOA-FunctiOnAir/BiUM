using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ApplicationExtensions
{
    public static IServiceCollection ConfigureCoreServices(this IServiceCollection services, Assembly assembly)
    {
        return services;
    }
}