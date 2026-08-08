using BiUM.Core.MessageBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQUtils
{
    private static readonly Type EventHandlerType = typeof(IEventHandler<>);

    // L-3: scan all assemblies once and cache the result; GetTypes() wrapped in try-catch to handle
    // assemblies that fail to load (ReflectionTypeLoadException) without crashing the entire scan.
    private static readonly Lazy<IReadOnlyList<(Type Implementation, Type Interface, Type Event)>> _allHandlers
        = new(BuildHandlerList, isThreadSafe: true);

    public static IEnumerable<(Type Implementation, Type Interface, Type Event)> GetAllHandlers()
        => _allHandlers.Value;

    private static IReadOnlyList<(Type Implementation, Type Interface, Type Event)> BuildHandlerList()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(t =>
            (
                t,
                t.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == EventHandlerType)
            ))
            .Where(x => x.Item2 is not null)
            .Select(x => (x.Item1, x.Item2!, x.Item2!.GenericTypeArguments[0]))
            .ToList();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Select(t => t!);
        }
    }
}