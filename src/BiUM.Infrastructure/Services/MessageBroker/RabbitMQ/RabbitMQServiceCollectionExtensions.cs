using BiUM.Infrastructure.Common.Events;
using Microsoft.Extensions.DependencyInjection;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventHandlers(this IServiceCollection services)
    {
        var handlerInterfaceType = typeof(IEventHandler<>);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Select(t => new
            {
                Type = t,
                HandlerInterface = t.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
            })
            .Where(x => x.HandlerInterface is not null)
            .ToList();

        foreach (var handler in handlerTypes)
        {
            services.AddScoped(handler.HandlerInterface!, handler.Type);
        }

        return services;
    }
}