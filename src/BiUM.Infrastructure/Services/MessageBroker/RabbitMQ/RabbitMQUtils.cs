using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.MessageBroker;
using System;
using System.Linq;
using System.Reflection;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQUtils
{
    public static readonly Type EventHandlerType = typeof(IEventHandler<>);
    private static string QueueTemplate = "{{owner}}/{{message}}";

    private const string BiappPrefix = "biapp-";

    public static string GetOwner(EventAttribute? attr, BiAppOptions biAppOptions)
    {
        if (string.IsNullOrEmpty(attr?.Owner))
        {
            return $"{BiappPrefix}{biAppOptions.Domain.ToLowerInvariant()}";
        }

        return attr.Owner.StartsWith(BiappPrefix, StringComparison.OrdinalIgnoreCase)
            ? attr.Owner
            : $"{BiappPrefix}{attr.Owner.ToLowerInvariant()}";
    }

    public static string? GetTarget(EventAttribute? attr)
    {
        if (string.IsNullOrEmpty(attr?.Target))
        {
            return null;
        }

        return attr.Target.StartsWith(BiappPrefix, StringComparison.OrdinalIgnoreCase)
            ? attr.Target
            : $"{BiappPrefix}{attr.Target.ToLowerInvariant()}";
    }

    public static Type MakeGenericType(Type type)
    {
        return EventHandlerType.MakeGenericType(type);
    }

    public static Type? GetInterface(Type? type)
    {
        return type?.GetInterface("IEventHandler`1")!.GenericTypeArguments[0];
    }

    public static (Type eventType, Type handlerInterface) GetInterfaceAndGenericType(Type type)
    {
        var iface = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == EventHandlerType);

        var eventType = iface.GenericTypeArguments[0];

        return (eventType, iface);
    }

    public static Type[] GetAllHandlerTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == EventHandlerType))
            .ToArray();
    }

    public static string ToConsumerName(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("Domain cannot be null or empty", nameof(domain));
        }

        return "biapp-" + SnakeCase(domain);
    }

    public static string GetQueueName(Type type, BiAppOptions biAppOptions, string? target = null)
    {
        var attribute = type.GetCustomAttribute<EventAttribute>();
        var message = SnakeCase(type.Name);

        if (attribute?.Mode == EventDeliveryMode.Publish)
        {
            throw new InvalidOperationException(
                $"GetQueueName cannot be used for Publish events. Event: {type.FullName}");
        }

        var owner = GetOwner(attribute, biAppOptions);

        var resolvedTarget = GetTarget(attribute);
        if (!string.IsNullOrEmpty(resolvedTarget))
        {
            owner += "__" + resolvedTarget;
        }
        else if (!string.IsNullOrEmpty(target))
        {
            var normalizedTarget = target.StartsWith(BiappPrefix, StringComparison.OrdinalIgnoreCase)
                ? target
                : $"{BiappPrefix}{target.ToLowerInvariant()}";
            owner += "__" + normalizedTarget;
        }

        return QueueTemplate
            .Replace("{{owner}}", owner)
            .Replace("{{message}}", message)
            .ToLowerInvariant();
    }

    public static string SnakeCase(string value)
        => string.Concat(value.Select((x, i) =>
                i > 0 && char.IsUpper(x) ? "_" + x : x.ToString()))
            .ToLowerInvariant();
}
