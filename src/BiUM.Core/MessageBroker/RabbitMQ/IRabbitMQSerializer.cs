using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker.RabbitMQ;

public interface IRabbitMQSerializer
{
    Task<ReadOnlyMemory<byte>> SerializeAsync(object value, Type type, CancellationToken cancellationToken = default);
    Task<object?> DeserializeAsync(ReadOnlyMemory<byte> value, Type type, CancellationToken cancellationToken = default);
}