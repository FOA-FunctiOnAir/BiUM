using SimpleHtmlToPdf;
using SimpleHtmlToPdf.Interfaces;
using SimpleHtmlToPdf.UnmanagedHandler;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, Assembly assembly)
    {
        return services;
    }

    public static IServiceCollection AddFileServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddSingleton<BindingWrapper>();
        services.AddSingleton<IConverter, HtmlConverter>();

        return services;
    }
}