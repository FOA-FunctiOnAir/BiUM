namespace BiUM.Infrastructure.Common.Events;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EventAttribute : Attribute
{
    public string Microservice { get; }

    public EventAttribute(string microservice)
    {
        Microservice = microservice;
    }
}