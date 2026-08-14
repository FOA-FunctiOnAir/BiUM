using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQClient
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : IBaseEvent;

    // Runtime-typed publish — for callers that only have an IBaseEvent instance (e.g. after
    // deserializing a buffered/pending event), not a compile-time T.
    Task PublishAsync(IBaseEvent message, CancellationToken cancellationToken = default);

    // Aktif bir compensation session varsa event hemen atılmaz, session commit olana kadar
    // ertelenir (IPendingEventStore ile); session yoksa PublishAsync ile aynı davranır.
    Task PublishAfterCommitAsync<T>(T message, CancellationToken cancellationToken = default) where T : IBaseEvent;

    Task PublishToDomainAsync<T>(string domain, T message, CancellationToken cancellationToken = default) where T : IBaseEvent;

    Task StartConsumingAsync(Type eventType, Type handlerType, CancellationToken cancellationToken = default);
}