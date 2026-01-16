using System;

namespace BiUM.Contract.Models.MessageBroker.RabbitMQ;

public sealed class Message
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime Timestamp { get; set; }
}
