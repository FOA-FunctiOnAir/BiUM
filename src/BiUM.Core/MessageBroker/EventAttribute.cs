using System;

namespace BiUM.Core.MessageBroker;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventAttribute : Attribute
{
    public string? Exchange { get; init; }
}