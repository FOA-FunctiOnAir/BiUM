using BiUM.Core.Models.MessageBroker.RabbitMQ;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient : IAsyncDisposable
{
    Task PublishAsync<T>(T message);
    Task PublishAsync<T>(string target, T message);
    Task SendMessageAsync(Message message, string exchangeName = "", string queueName = "", bool persistent = false);
    Task<T?> ReceiveMessageAsync<T>(CancellationToken token);
    Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token);
    Task<Message> ReceiveMessageAsync(string queueName = "");
    Task StartConsumingAsync(Type eventType, Func<object, Task> callback);
}
