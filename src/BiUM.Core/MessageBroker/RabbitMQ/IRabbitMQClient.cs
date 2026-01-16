using BiUM.Contract.Models.MessageBroker.RabbitMQ;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient
{
    Task PublishAsync<T>(T message) where T : IBaseEvent;
    Task PublishAsync<T>(string target, T message) where T : IBaseEvent;
    Task SendMessageAsync(Message message, string exchangeName = "", string queueName = "", bool persistent = false);
    Task<T?> ReceiveMessageAsync<T>(CancellationToken token);
    Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token);
    Task<Message> ReceiveMessageAsync(string queueName = "");
    Task StartConsumingAsync(Type eventType, Func<object, Task> callback, string consumerName);
}
