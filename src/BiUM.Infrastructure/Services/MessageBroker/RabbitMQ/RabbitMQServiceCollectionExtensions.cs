using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventHandlers(this IServiceCollection services)
    {
        var handlerTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t => new
            {
                Implementation = t,
                Interface = t.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == RabbitMQUtils.EventHandlerType)
            })
            .Where(x => x.Interface is not null);

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.Interface!, handler.Implementation);
        }

        return services;
    }
}
