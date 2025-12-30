using System;

namespace BiUM.Core.MessageBroker;

[Event("observability")]
public class ServiceCalledEvent : BaseEvent
{
    public Guid? MicroserviceId { get; set; }
    public Guid? ServiceId { get; set; }
    public string ServiceName { get; set; }
    public string Endpoint { get; set; }
    public string HttpMethod { get; set; }
    public long ExecutionTimeMs { get; set; }
    public bool Success { get; set; }
}
