using BiUM.Core.MessageBroker;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQUtils
{
    private static readonly Type EventHandlerType = typeof(IEventHandler<>);

    public static IEnumerable<(Type Implementation, Type Interface, Type Event)> GetAllHandlers()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t =>
            (
                t,
                t.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == EventHandlerType)
            ))
            .Where(x => x.Item2 is not null)
            .Select(x => (x.Item1, x.Item2!, x.Item2!.GenericTypeArguments[0]));
    }
}