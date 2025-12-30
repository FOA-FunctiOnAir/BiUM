using BiUM.Core.Common.Enums;
using System;

namespace BiUM.Core.MessageBroker;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventAttribute : Attribute
{
    public string? Owner { get; }
    public string? Target { get; }
    public EventDeliveryMode Mode { get; }

    public EventAttribute()
    {
        Mode = EventDeliveryMode.Publish;
    }

    public EventAttribute(string owner)
    {
        Owner = owner;
        Mode = EventDeliveryMode.Publish;
    }

    public EventAttribute(string owner, string target)
    {
        Owner = owner;
        Target = target;
        Mode = EventDeliveryMode.Targeted;
    }

    public EventAttribute(string owner, EventDeliveryMode mode)
    {
        Owner = owner;
        Mode = mode;
    }

    public EventAttribute(string owner, EventDeliveryMode mode, string target)
    {
        Owner = owner;
        Mode = mode;
        Target = target;
    }
}
