using BiUM.Core.Models.MessageBroker.RabbitMQ;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient : IAsyncDisposable
{
    public Task PublishAsync<T>(T message) where T : IBaseEvent;
    public Task PublishAsync<T>(string target, T message) where T : IBaseEvent;
    public Task SendMessageAsync(Message message, string exchangeName = "", string queueName = "", bool persistent = false);
    public Task<T?> ReceiveMessageAsync<T>(CancellationToken token);
    public Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token);
    public Task<Message> ReceiveMessageAsync(string queueName = "");
    public Task StartConsumingAsync(Type eventType, Func<object, Task> callback, string consumerName);
}
