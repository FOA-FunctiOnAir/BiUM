using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IBaseEvent;
    Task StartConsumingAsync(Type eventType, Type handlerType, CancellationToken cancellationToken = default);
}