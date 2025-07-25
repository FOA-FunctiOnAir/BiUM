namespace BiUM.Infrastructure.Common.Events;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventAttribute : Attribute
{
    public string Owner { get; }
    public string Target { get; }

    public EventAttribute(string owner)
    {
        Owner = owner;
    }

    public EventAttribute(string owner, string target)
    {
        Owner = owner;
        Target = target;
    }
}