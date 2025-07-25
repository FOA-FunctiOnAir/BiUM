using BiUM.Core.Models.MessageBroker.RabbitMQ;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient
{
    Task PublishAsync<T>(T message);
    void SendMessage(Message message, string exchangeName = "", string queueName = "", bool persistent = false);
    Task<T?> ReceiveMessageAsync<T>(CancellationToken token);
    Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token);
    Task<Message> ReceiveMessageAsync(string queueName = "");
    void Dispose();
}